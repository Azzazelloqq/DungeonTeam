using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace Code.ApplicationRoot
{
    internal static class GuildProfileSnapshotBuilder
    {
        public static GuildProfileSnapshot Build(
            PlayerProfileState profile,
            ActorConfigCatalog actors,
            SkillCatalog skills,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            ItemCatalog itemCatalog = null,
            int receptionRewardCount = 0,
            GuildTextSnapshot rewardsAction = null)
        {
            return Build(profile, actors, skills, teamSetup, text, itemCatalog, null, receptionRewardCount, rewardsAction);
        }

        public static GuildProfileSnapshot Build(
            PlayerProfileState profile,
            ActorConfigCatalog actors,
            SkillCatalog skills,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            ItemCatalog itemCatalog,
            GuildRankCatalog rankCatalog,
            int receptionRewardCount = 0,
            GuildTextSnapshot rewardsAction = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (teamSetup == null)
            {
                throw new ArgumentNullException(nameof(teamSetup));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var resolver = itemCatalog != null ? new EquipmentEffectResolver(itemCatalog) : null;
            resolver?.ValidateInventory(profile.Inventory);
            var roster = new GuildHeroSnapshot[profile.Heroes.Count];
            GuildHeroSnapshot leader = null;
            var companions = new GuildHeroSnapshot[profile.CompanionActorIds.Count];

            for (var index = 0; index < profile.Heroes.Count; index++)
            {
                var hero = profile.Heroes[index];
                var role = ResolveRole(profile, hero.ActorId);
                var snapshot = BuildHero(hero, role, actors, skills, teamSetup, text, profile, itemCatalog, resolver);
                roster[index] = snapshot;

                if (role == GuildHeroRole.Leader)
                {
                    leader = snapshot;
                }
            }

            for (var index = 0; index < companions.Length; index++)
            {
                companions[index] = RequireHero(roster, profile.CompanionActorIds[index]);
            }

            var rank = BuildRankSnapshot(profile, rankCatalog);
            return new GuildProfileSnapshot(
                profile.Gold,
                rank?.CurrentDisplayName ?? text.UnassignedRank.DisplayText,
                leader ?? throw new InvalidOperationException("Profile leader was not resolved."),
                companions,
                roster,
                text,
                BuildResourceRows(profile, itemCatalog),
                rank,
                receptionRewardCount,
                rewardsAction);
        }

        private static GuildRankSnapshot BuildRankSnapshot(
            PlayerProfileState profile,
            GuildRankCatalog rankCatalog)
        {
            if (rankCatalog == null)
            {
                return null;
            }

            var current = rankCatalog.Require(profile.RankId);
            if (!rankCatalog.TryGetNext(current.RankId, out var next))
            {
                return new GuildRankSnapshot(
                    current.RankId,
                    current.DisplayName,
                    null,
                    null,
                    false);
            }

            return new GuildRankSnapshot(
                current.RankId,
                current.DisplayName,
                next.DisplayName,
                next.PromotionCost,
                profile.Gold >= next.PromotionCost);
        }

        private static GuildHeroRole ResolveRole(PlayerProfileState profile, string actorId)
        {
            if (string.Equals(actorId, profile.LeaderActorId, StringComparison.Ordinal))
            {
                return GuildHeroRole.Leader;
            }

            for (var index = 0; index < profile.CompanionActorIds.Count; index++)
            {
                if (string.Equals(
                        actorId,
                        profile.CompanionActorIds[index],
                        StringComparison.Ordinal))
                {
                    return GuildHeroRole.Companion;
                }
            }

            return GuildHeroRole.Available;
        }

        private static GuildHeroSnapshot BuildHero(
            HeroProfileState hero,
            GuildHeroRole role,
            ActorConfigCatalog actors,
            SkillCatalog skills,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            PlayerProfileState profile,
            ItemCatalog itemCatalog,
            EquipmentEffectResolver resolver)
        {
            var actor = actors.Require(hero.ActorId);
            var stats = actors.Resolve(hero.ActorId, hero.Level);
            var effect = resolver != null
                ? resolver.Resolve(profile.Inventory, hero.ActorId)
                : EquipmentEffectSnapshot.Zero;
            var loadout = skills.RequireLoadout(hero.LoadoutId);
            var skillRows = new GuildHeroSkillSnapshot[loadout.Slots.Count];

            for (var index = 0; index < skillRows.Length; index++)
            {
                var resolved = skills.Resolve(hero.LoadoutId, loadout.Slots[index].Slot);
                skillRows[index] = new GuildHeroSkillSnapshot(
                    resolved.Slot.ToString(),
                    GetSlotDisplayText(resolved.Slot, text),
                    resolved.Skill.DisplayName,
                    resolved.Level.Level);
            }

            var member = RequireMember(teamSetup, hero.ActorId);
            if (!member.SupportsLevel(hero.Level))
            {
                throw new InvalidOperationException(
                    $"Profile actor '{hero.ActorId}' level {hero.Level} is unavailable for this run.");
            }

            if (!member.SupportsLoadout(hero.LoadoutId))
            {
                throw new InvalidOperationException(
                    $"Profile actor '{hero.ActorId}' loadout '{hero.LoadoutId}' is unavailable for this run.");
            }

            var allowedLoadouts = new GuildHeroLoadoutSnapshot[member.AvailableLoadoutIds.Count];
            for (var index = 0; index < allowedLoadouts.Length; index++)
            {
                var loadoutId = member.AvailableLoadoutIds[index];
                var option = skills.RequireLoadout(loadoutId);
                allowedLoadouts[index] = new GuildHeroLoadoutSnapshot(
                    loadoutId,
                    BuildLoadoutDisplayText(option, skills, text));
            }

            return new GuildHeroSnapshot(
                hero.ActorId,
                actor.DisplayName,
                role,
                hero.Level,
                checked(stats.MaximumHealth + effect.MaximumHealthBonus),
                stats.MovementSpeed + effect.MovementSpeedBonus,
                skillRows,
                hero.LoadoutId,
                allowedLoadouts,
                BuildEquipmentRows(profile.Inventory, hero.ActorId, itemCatalog),
                BuildInventoryRows(profile.Inventory, hero.ActorId, itemCatalog));
        }

        private static GuildEquipmentSlotSnapshot[] BuildEquipmentRows(
            InventoryState inventory,
            string actorId,
            ItemCatalog itemCatalog)
        {
            if (itemCatalog == null || !inventory.TryGetEquipment(actorId, out var equipment))
                return Array.Empty<GuildEquipmentSlotSnapshot>();
            return new[]
            {
                BuildEquipmentRow(GuildProfileEquipmentSlot.Weapon, EquipmentSlot.Weapon, equipment.WeaponInstanceId, inventory, itemCatalog),
                BuildEquipmentRow(GuildProfileEquipmentSlot.Armor, EquipmentSlot.Armor, equipment.ArmorInstanceId, inventory, itemCatalog),
                BuildEquipmentRow(GuildProfileEquipmentSlot.Relic, EquipmentSlot.Relic, equipment.RelicInstanceId, inventory, itemCatalog)
            };
        }

        private static GuildEquipmentSlotSnapshot BuildEquipmentRow(
            GuildProfileEquipmentSlot guildSlot,
            EquipmentSlot slot,
            string instanceId,
            InventoryState inventory,
            ItemCatalog itemCatalog)
        {
            var display = slot.ToString();
            var itemName = string.Empty;
            if (!string.IsNullOrWhiteSpace(instanceId) && inventory.TryGetInstance(instanceId, out var item) && itemCatalog.TryGetEquipment(item.DefinitionId, out var definition))
                itemName = definition.DisplayName;
            return new GuildEquipmentSlotSnapshot(guildSlot, display, instanceId, itemName);
        }

        private static GuildInventoryItemSnapshot[] BuildInventoryRows(
            InventoryState inventory,
            string actorId,
            ItemCatalog itemCatalog)
        {
            if (itemCatalog == null) return Array.Empty<GuildInventoryItemSnapshot>();
            var rows = new List<GuildInventoryItemSnapshot>();
            for (var index = 0; index < inventory.UniqueItems.Count; index++)
            {
                var item = inventory.UniqueItems[index];
                if (!itemCatalog.TryGetEquipment(item.DefinitionId, out var definition))
                    continue;
                var equipped = inventory.TryGetEquipment(actorId, out var equipment) &&
                    (string.Equals(equipment.WeaponInstanceId, item.InstanceId, StringComparison.Ordinal) ||
                     string.Equals(equipment.ArmorInstanceId, item.InstanceId, StringComparison.Ordinal) ||
                     string.Equals(equipment.RelicInstanceId, item.InstanceId, StringComparison.Ordinal));
                rows.Add(new GuildInventoryItemSnapshot(
                    item.InstanceId,
                    item.DefinitionId,
                    definition.DisplayName,
                    ToGuildSlot(definition.Slot),
                    equipped,
                    definition.SaleValue,
                    definition.IsEligibleFor(actorId)));
            }
            return rows.ToArray();
        }

        private static GuildResourceSnapshot[] BuildResourceRows(PlayerProfileState profile, ItemCatalog itemCatalog)
        {
            var rows = new GuildResourceSnapshot[profile.Inventory.Resources.Count];
            for (var index = 0; index < rows.Length; index++)
            {
                var resource = profile.Inventory.Resources[index];
                ResourceItemDefinition definition = null;
                var hasDefinition = itemCatalog != null && itemCatalog.TryGetResource(resource.DefinitionId, out definition);
                var display = hasDefinition
                    ? definition.DisplayName
                    : resource.DefinitionId;
                var saleValue = hasDefinition
                    ? checked((long)definition.SaleValue * resource.Quantity)
                    : 0L;
                rows[index] = new GuildResourceSnapshot(resource.DefinitionId, display, resource.Quantity, saleValue);
            }
            return rows;
        }

        private static GuildProfileEquipmentSlot ToGuildSlot(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => GuildProfileEquipmentSlot.Weapon,
            EquipmentSlot.Armor => GuildProfileEquipmentSlot.Armor,
            EquipmentSlot.Relic => GuildProfileEquipmentSlot.Relic,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };

        private static string GetSlotDisplayText(
            SkillSlot slot,
            GuildProfileTextSnapshot text)
        {
            return slot switch
            {
                SkillSlot.Primary => text.PrimarySkillLabel.DisplayText,
                SkillSlot.Active1 => text.ActiveSkillLabel.DisplayText,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
            };
        }

        private static GuildHeroSnapshot RequireHero(
            GuildHeroSnapshot[] roster,
            string actorId)
        {
            for (var index = 0; index < roster.Length; index++)
            {
                if (string.Equals(roster[index].ActorId, actorId, StringComparison.Ordinal))
                {
                    return roster[index];
                }
            }

            throw new InvalidOperationException(
                $"Profile companion '{actorId}' was not resolved in the roster.");
        }

        private static DungeonRunTeamMemberOption RequireMember(
            DungeonRunTeamSetup teamSetup,
            string actorId)
        {
            for (var index = 0; index < teamSetup.Members.Count; index++)
            {
                if (string.Equals(teamSetup.Members[index].ActorId, actorId, StringComparison.Ordinal))
                {
                    return teamSetup.Members[index];
                }
            }

            throw new InvalidOperationException($"Profile actor '{actorId}' is unavailable for this run.");
        }

        private static string BuildLoadoutDisplayText(
            CombatLoadoutDefinition loadout,
            SkillCatalog skills,
            GuildProfileTextSnapshot text)
        {
            var result = text.LoadoutLabel.DisplayText + ": ";
            for (var index = 0; index < loadout.Slots.Count; index++)
            {
                if (index > 0)
                {
                    result += " / ";
                }

                result += skills.RequireSkill(loadout.Slots[index].SkillId).DisplayName;
            }

            return result;
        }
    }
}

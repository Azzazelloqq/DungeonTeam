using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;

namespace Code.ApplicationRoot
{
    internal static class GuildProfileSnapshotBuilder
    {
        public static GuildProfileSnapshot Build(
            PlayerProfileState profile,
            ActorConfigCatalog actors,
            SkillCatalog skills,
            GuildProfileTextSnapshot text)
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

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var roster = new GuildHeroSnapshot[profile.Heroes.Count];
            GuildHeroSnapshot leader = null;
            var companions = new GuildHeroSnapshot[profile.CompanionActorIds.Count];

            for (var index = 0; index < profile.Heroes.Count; index++)
            {
                var hero = profile.Heroes[index];
                var role = ResolveRole(profile, hero.ActorId);
                var snapshot = BuildHero(hero, role, actors, skills, text);
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

            return new GuildProfileSnapshot(
                profile.Gold,
                text.UnassignedRank.DisplayText,
                leader ?? throw new InvalidOperationException("Profile leader was not resolved."),
                companions,
                roster,
                text);
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
            GuildProfileTextSnapshot text)
        {
            var actor = actors.Require(hero.ActorId);
            var stats = actors.Resolve(hero.ActorId, hero.Level);
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

            return new GuildHeroSnapshot(
                hero.ActorId,
                actor.DisplayName,
                role,
                hero.Level,
                stats.MaximumHealth,
                stats.MovementSpeed,
                skillRows);
        }

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
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Infrastructure;
using LocalSaveSystem;
using UnityEngine;

namespace Code.ApplicationRoot
{
    internal static class PlayerProfileComposition
    {
        public static PlayerProfileSession Create(
            DungeonRunTeamSetup teamSetup,
            ItemCatalog itemCatalog,
            out PlayerProfilePersistence persistence)
        {
            if (teamSetup == null)
            {
                throw new ArgumentNullException(nameof(teamSetup));
            }

            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            var defaultSelection = teamSetup.DefaultSelection;
            var heroes = new HeroProfileState[defaultSelection.MemberCount];
            heroes[0] = ToProfileHero(defaultSelection.Leader);
            for (var index = 0; index < defaultSelection.Companions.Count; index++)
            {
                heroes[index + 1] = ToProfileHero(defaultSelection.Companions[index]);
            }

            var companions = new string[defaultSelection.CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++)
            {
                companions[index] = defaultSelection.CompanionActorIds[index];
            }

            var rosterActorIds = new string[heroes.Length];
            for (var index = 0; index < rosterActorIds.Length; index++)
            {
                rosterActorIds[index] = heroes[index].ActorId;
            }

            var starterInventory = itemCatalog.CreateStarterInventory(rosterActorIds);
            var seed = new PlayerProfileSeed(
                heroes,
                defaultSelection.LeaderActorId,
                companions,
                starterInventory);
            var storagePath = Path.Combine(Application.persistentDataPath, "DungeonTeam");
            Directory.CreateDirectory(storagePath);
            persistence = new PlayerProfilePersistence(new SaveStoreOptions(storagePath)
            {
                UseTaggedFormat = true,
                UseAtomicWrite = true,
                SaveOnQuit = true
            }, heroesToInventory => CreateStarterInventory(itemCatalog, heroesToInventory));
            try
            {
                var session = new PlayerProfileSession(
                    persistence.Repository,
                    seed);
                teamSetup.RequireValid(MapToTeamSelection(session.State, itemCatalog));
                return session;
            }
            catch
            {
                persistence.Dispose();
                persistence = null;
                throw;
            }
        }

        public static DungeonRunTeamSelection MapToTeamSelection(PlayerProfileState profile)
        {
            return MapToTeamSelection(profile, null);
        }

        public static DungeonRunTeamSelection MapToTeamSelection(
            PlayerProfileState profile,
            ItemCatalog itemCatalog)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var resolver = itemCatalog != null ? new EquipmentEffectResolver(itemCatalog) : null;
            resolver?.ValidateInventory(profile.Inventory);
            var leaderHero = RequireHero(profile, profile.LeaderActorId);
            var leader = ToSelection(leaderHero, resolver?.Resolve(profile.Inventory, leaderHero.ActorId));
            var companions = new DungeonRunActorSelection[profile.CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++)
            {
                var companion = RequireHero(profile, profile.CompanionActorIds[index]);
                companions[index] = ToSelection(
                    companion,
                    resolver?.Resolve(profile.Inventory, companion.ActorId));
            }

            return new DungeonRunTeamSelection(leader, companions);
        }

        private static HeroProfileState ToProfileHero(DungeonRunActorSelection actor) =>
            new(actor.ActorId, actor.Level, actor.LoadoutId);

        private static InventoryState CreateStarterInventory(
            ItemCatalog itemCatalog,
            IReadOnlyList<HeroProfileState> heroes)
        {
            var actorIds = new string[heroes.Count];
            for (var index = 0; index < actorIds.Length; index++)
            {
                actorIds[index] = heroes[index].ActorId;
            }

            return itemCatalog.CreateStarterInventory(actorIds);
        }

        private static DungeonRunActorSelection ToSelection(
            HeroProfileState hero,
            EquipmentEffectSnapshot? effect = null) =>
            new(
                hero.ActorId,
                hero.Level,
                hero.LoadoutId,
                effect.HasValue
                    ? new DungeonRunActorBonus(
                        effect.Value.PrimaryDamageBonus,
                        effect.Value.MaximumHealthBonus,
                        effect.Value.MovementSpeedBonus)
                    : DungeonRunActorBonus.Zero);

        private static HeroProfileState RequireHero(PlayerProfileState profile, string actorId)
        {
            for (var index = 0; index < profile.Heroes.Count; index++)
            {
                if (string.Equals(profile.Heroes[index].ActorId, actorId, StringComparison.Ordinal))
                {
                    return profile.Heroes[index];
                }
            }

            throw new InvalidOperationException($"Profile actor '{actorId}' is missing from roster.");
        }
    }

}

using System;
using System.IO;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Infrastructure;
using LocalSaveSystem;
using UnityEngine;

namespace Code.ApplicationRoot
{
    internal static class PlayerProfileComposition
    {
        public static PlayerProfileSession Create(DungeonRunTeamSetup teamSetup, out SaveStore store)
        {
            if (teamSetup == null)
            {
                throw new ArgumentNullException(nameof(teamSetup));
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

            var seed = new PlayerProfileSeed(heroes, defaultSelection.LeaderActorId, companions);
            var storagePath = Path.Combine(Application.persistentDataPath, "DungeonTeam");
            Directory.CreateDirectory(storagePath);
            store = new SaveStore(new SaveStoreOptions(storagePath)
            {
                UseTaggedFormat = true,
                UseAtomicWrite = true,
                SaveOnQuit = true
            });
            var session = new PlayerProfileSession(
                new SaveStorePlayerProfileRepository(store),
                seed);
            teamSetup.RequireValid(MapToTeamSelection(session.State));
            return session;
        }

        public static DungeonRunTeamSelection MapToTeamSelection(PlayerProfileState profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var leader = ToSelection(RequireHero(profile, profile.LeaderActorId));
            var companions = new DungeonRunActorSelection[profile.CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++)
            {
                companions[index] = ToSelection(RequireHero(
                    profile,
                    profile.CompanionActorIds[index]));
            }

            return new DungeonRunTeamSelection(leader, companions);
        }

        private static HeroProfileState ToProfileHero(DungeonRunActorSelection actor) =>
            new(actor.ActorId, actor.Level, actor.LoadoutId);

        private static DungeonRunActorSelection ToSelection(HeroProfileState hero) =>
            new(hero.ActorId, hero.Level, hero.LoadoutId);

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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.PlayerProfile.Domain;

namespace DungeonTeam.Gameplay.PlayerProfile.Application
{
    public interface IPlayerProfileRepository
    {
        bool TryLoad(out PlayerProfileState state);
        void Save(PlayerProfileState state);
    }

    public sealed class PlayerProfileSeed
    {
        public PlayerProfileSeed(
            IReadOnlyList<HeroProfileState> heroes,
            string leaderActorId,
            IReadOnlyList<string> companionActorIds)
        {
            State = new PlayerProfileState(0, null, heroes, leaderActorId, companionActorIds);
        }

        public PlayerProfileState State { get; }
    }

    public sealed class PlayerProfileSession
    {
        public PlayerProfileSession(IPlayerProfileRepository repository, PlayerProfileSeed seed)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            if (!repository.TryLoad(out var state))
            {
                state = seed.State;
                repository.Save(state);
            }

            State = state ?? throw new InvalidOperationException("Loaded player profile is missing.");
        }

        public PlayerProfileState State { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.PlayerProfile.Domain
{
    public readonly struct HeroProfileState
    {
        public HeroProfileState(string actorId, int level, string loadoutId)
        {
            ActorId = Require(actorId, nameof(actorId));
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
            LoadoutId = Require(loadoutId, nameof(loadoutId));
        }

        public string ActorId { get; }
        public int Level { get; }
        public string LoadoutId { get; }

        private static string Require(string value, string name) => !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Profile ID cannot be empty.", name);
    }

    public sealed class PlayerProfileState
    {
        private readonly ReadOnlyCollection<HeroProfileState> _heroes;
        private readonly ReadOnlyCollection<string> _companionActorIds;

        public PlayerProfileState(
            long gold,
            string rankId,
            IReadOnlyList<HeroProfileState> heroes,
            string leaderActorId,
            IReadOnlyList<string> companionActorIds)
        {
            if (gold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gold));
            }

            if (rankId != null && string.IsNullOrWhiteSpace(rankId))
            {
                throw new ArgumentException(
                    "Rank ID cannot be empty when set.",
                    nameof(rankId));
            }

            if (heroes == null || heroes.Count == 0)
            {
                throw new ArgumentException("Profile roster cannot be empty.", nameof(heroes));
            }

            if (companionActorIds == null)
            {
                throw new ArgumentNullException(nameof(companionActorIds));
            }

            var heroCopy = new HeroProfileState[heroes.Count];
            var roster = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (!roster.Add(hero.ActorId))
                {
                    throw new ArgumentException(
                        $"Actor ID '{hero.ActorId}' is duplicated.",
                        nameof(heroes));
                }

                heroCopy[i] = hero;
            }

            LeaderActorId = !string.IsNullOrWhiteSpace(leaderActorId) && roster.Contains(leaderActorId)
                ? leaderActorId
                : throw new ArgumentException("Leader must belong to roster.", nameof(leaderActorId));
            var companionCopy = new string[companionActorIds.Count];
            var companions = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < companionActorIds.Count; i++)
            {
                var id = companionActorIds[i];
                if (string.IsNullOrWhiteSpace(id) || !roster.Contains(id) || id == LeaderActorId || !companions.Add(id))
                {
                    throw new ArgumentException("Companions must be unique roster members other than leader.", nameof(companionActorIds));
                }

                companionCopy[i] = id;
            }

            Gold = gold;
            RankId = rankId;
            _heroes = Array.AsReadOnly(heroCopy);
            _companionActorIds = Array.AsReadOnly(companionCopy);
        }

        public long Gold { get; }
        public string RankId { get; }
        public IReadOnlyList<HeroProfileState> Heroes => _heroes;
        public string LeaderActorId { get; }
        public IReadOnlyList<string> CompanionActorIds => _companionActorIds;
    }
}

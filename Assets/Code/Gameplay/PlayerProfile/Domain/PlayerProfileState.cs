using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Inventory.Domain;

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
            : this(
                gold,
                rankId,
                heroes,
                leaderActorId,
                companionActorIds,
                InventoryState.Empty)
        {
        }

        public PlayerProfileState(
            long gold,
            string rankId,
            IReadOnlyList<HeroProfileState> heroes,
            string leaderActorId,
            IReadOnlyList<string> companionActorIds,
            InventoryState inventory)
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

            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            for (var index = 0; index < Inventory.EquipmentByHero.Count; index++)
            {
                if (!roster.Contains(Inventory.EquipmentByHero[index].ActorId))
                {
                    throw new ArgumentException(
                        "Inventory equipment mappings must belong to the profile roster.",
                        nameof(inventory));
                }
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
        public InventoryState Inventory { get; }

        public PlayerProfileState ReplaceInventory(InventoryState inventory)
        {
            return new PlayerProfileState(
                Gold,
                RankId,
                Heroes,
                LeaderActorId,
                CompanionActorIds,
                inventory ?? throw new ArgumentNullException(nameof(inventory)));
        }

        public PlayerProfileState ChangeLeader(string actorId)
        {
            RequireRosterActor(actorId, nameof(actorId));
            if (string.Equals(actorId, LeaderActorId, StringComparison.Ordinal))
            {
                return this;
            }

            var companions = CopyCompanions();
            var selectedCompanionIndex = IndexOf(companions, actorId);
            if (selectedCompanionIndex >= 0)
            {
                companions[selectedCompanionIndex] = LeaderActorId;
            }

            return new PlayerProfileState(Gold, RankId, Heroes, actorId, companions, Inventory);
        }

        public PlayerProfileState AddCompanion(string actorId)
        {
            RequireRosterActor(actorId, nameof(actorId));
            if (string.Equals(actorId, LeaderActorId, StringComparison.Ordinal) ||
                IndexOf(CompanionActorIds, actorId) >= 0)
            {
                throw new ArgumentException("Actor is already in the active team.", nameof(actorId));
            }

            var companions = new string[CompanionActorIds.Count + 1];
            for (var index = 0; index < CompanionActorIds.Count; index++)
            {
                companions[index] = CompanionActorIds[index];
            }

            companions[^1] = actorId;
            return new PlayerProfileState(Gold, RankId, Heroes, LeaderActorId, companions, Inventory);
        }

        public PlayerProfileState RemoveCompanion(string actorId)
        {
            var removeIndex = IndexOf(CompanionActorIds, actorId);
            if (removeIndex < 0)
            {
                throw new ArgumentException("Actor is not a companion.", nameof(actorId));
            }

            var companions = new string[CompanionActorIds.Count - 1];
            for (int sourceIndex = 0, targetIndex = 0;
                 sourceIndex < CompanionActorIds.Count;
                 sourceIndex++)
            {
                if (sourceIndex != removeIndex)
                {
                    companions[targetIndex++] = CompanionActorIds[sourceIndex];
                }
            }

            return new PlayerProfileState(Gold, RankId, Heroes, LeaderActorId, companions, Inventory);
        }

        public PlayerProfileState ChangeLoadout(string actorId, string loadoutId)
        {
            if (string.IsNullOrWhiteSpace(loadoutId))
            {
                throw new ArgumentException("Loadout ID cannot be empty.", nameof(loadoutId));
            }

            var heroIndex = IndexOfHero(actorId);
            if (heroIndex < 0)
            {
                throw new ArgumentException("Actor must belong to roster.", nameof(actorId));
            }

            if (string.Equals(Heroes[heroIndex].LoadoutId, loadoutId, StringComparison.Ordinal))
            {
                return this;
            }

            var heroes = new HeroProfileState[Heroes.Count];
            for (var index = 0; index < heroes.Length; index++)
            {
                heroes[index] = index == heroIndex
                    ? new HeroProfileState(actorId, Heroes[index].Level, loadoutId)
                    : Heroes[index];
            }

            return new PlayerProfileState(Gold, RankId, heroes, LeaderActorId, CompanionActorIds, Inventory);
        }

        private void RequireRosterActor(string actorId, string parameterName)
        {
            if (IndexOfHero(actorId) < 0)
            {
                throw new ArgumentException("Actor must belong to roster.", parameterName);
            }
        }

        private int IndexOfHero(string actorId)
        {
            for (var index = 0; index < Heroes.Count; index++)
            {
                if (string.Equals(Heroes[index].ActorId, actorId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private string[] CopyCompanions()
        {
            var companions = new string[CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++)
            {
                companions[index] = CompanionActorIds[index];
            }

            return companions;
        }

        private static int IndexOf(IReadOnlyList<string> actorIds, string actorId)
        {
            for (var index = 0; index < actorIds.Count; index++)
            {
                if (string.Equals(actorIds[index], actorId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}

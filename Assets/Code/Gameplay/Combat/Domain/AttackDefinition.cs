using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Combat.Domain
{
    public readonly struct AttackRankDefinition
    {
        public AttackRankDefinition(int rank, int damage, float range, float cooldown)
        {
            Rank = rank > 0
                ? rank
                : throw new ArgumentOutOfRangeException(nameof(rank));
            Damage = damage > 0
                ? damage
                : throw new ArgumentOutOfRangeException(nameof(damage));
            Range = range > 0f
                ? range
                : throw new ArgumentOutOfRangeException(nameof(range));
            Cooldown = cooldown > 0f
                ? cooldown
                : throw new ArgumentOutOfRangeException(nameof(cooldown));
        }

        public int Rank { get; }
        public int Damage { get; }
        public float Range { get; }
        public float Cooldown { get; }
    }

    public sealed class AttackDefinition
    {
        private readonly ReadOnlyCollection<AttackRankDefinition> _ranks;
        private readonly Dictionary<int, AttackRankDefinition> _ranksByNumber;

        public AttackDefinition(
            string attackId,
            string displayName,
            IReadOnlyList<AttackRankDefinition> ranks)
        {
            AttackId = RequireValue(attackId, nameof(attackId));
            DisplayName = RequireValue(displayName, nameof(displayName));
            if (ranks == null)
            {
                throw new ArgumentNullException(nameof(ranks));
            }

            if (ranks.Count == 0)
            {
                throw new ArgumentException(
                    $"Attack '{AttackId}' requires at least one rank.",
                    nameof(ranks));
            }

            var copiedRanks = new AttackRankDefinition[ranks.Count];
            _ranksByNumber = new Dictionary<int, AttackRankDefinition>(ranks.Count);
            for (var index = 0; index < ranks.Count; index++)
            {
                var rank = ranks[index];
                if (!_ranksByNumber.TryAdd(rank.Rank, rank))
                {
                    throw new ArgumentException(
                        $"Attack '{AttackId}' contains rank {rank.Rank} more than once.",
                        nameof(ranks));
                }

                copiedRanks[index] = rank;
            }

            _ranks = Array.AsReadOnly(copiedRanks);
        }

        public string AttackId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<AttackRankDefinition> Ranks => _ranks;

        public AttackRankDefinition RequireRank(int rank)
        {
            if (!_ranksByNumber.TryGetValue(rank, out var definition))
            {
                throw new InvalidOperationException(
                    $"Attack '{AttackId}' does not contain rank {rank}.");
            }

            return definition;
        }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class CombatLoadoutDefinition
    {
        public CombatLoadoutDefinition(string loadoutId, string primaryAttackId)
        {
            LoadoutId = RequireValue(loadoutId, nameof(loadoutId));
            PrimaryAttackId = RequireValue(primaryAttackId, nameof(primaryAttackId));
        }

        public string LoadoutId { get; }
        public string PrimaryAttackId { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Combat.Domain;

namespace DungeonTeam.Gameplay.Combat.Runtime
{
    public sealed class CombatCatalog
    {
        private readonly Dictionary<string, AttackDefinition> _attacks;
        private readonly Dictionary<string, CombatLoadoutDefinition> _loadouts;

        public CombatCatalog(
            AttackDefinitionConfig[] attacks,
            CombatLoadoutDefinitionConfig[] loadouts)
        {
            if (attacks == null)
            {
                throw new ArgumentNullException(nameof(attacks));
            }

            if (loadouts == null)
            {
                throw new ArgumentNullException(nameof(loadouts));
            }

            _attacks = new Dictionary<string, AttackDefinition>(
                attacks.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < attacks.Length; index++)
            {
                var config = attacks[index] ?? throw new ArgumentException(
                    $"Attack definition at index {index} is missing.",
                    nameof(attacks));
                var definition = config.ToDomain(index);
                if (!_attacks.TryAdd(definition.AttackId, definition))
                {
                    throw new ArgumentException(
                        $"Attack ID '{definition.AttackId}' is configured more than once.",
                        nameof(attacks));
                }
            }

            _loadouts = new Dictionary<string, CombatLoadoutDefinition>(
                loadouts.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < loadouts.Length; index++)
            {
                var config = loadouts[index] ?? throw new ArgumentException(
                    $"Combat loadout at index {index} is missing.",
                    nameof(loadouts));
                var definition = config.ToDomain();
                if (!_attacks.ContainsKey(definition.PrimaryAttackId))
                {
                    throw new ArgumentException(
                        $"Combat loadout '{definition.LoadoutId}' references unknown primary " +
                        $"attack ID '{definition.PrimaryAttackId}'.",
                        nameof(loadouts));
                }

                if (!_loadouts.TryAdd(definition.LoadoutId, definition))
                {
                    throw new ArgumentException(
                        $"Combat loadout ID '{definition.LoadoutId}' is configured more than once.",
                        nameof(loadouts));
                }
            }
        }

        public AttackDefinition RequireAttack(string attackId)
        {
            return Require(_attacks, attackId, "Attack");
        }

        public CombatLoadoutDefinition RequireLoadout(string loadoutId)
        {
            return Require(_loadouts, loadoutId, "Combat loadout");
        }

        public AttackRankDefinition ResolvePrimaryAttack(string loadoutId, int rank)
        {
            var loadout = RequireLoadout(loadoutId);
            return RequireAttack(loadout.PrimaryAttackId).RequireRank(rank);
        }

        private static T Require<T>(
            IReadOnlyDictionary<string, T> definitions,
            string id,
            string label)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"{label} ID cannot be empty.", nameof(id));
            }

            if (!definitions.TryGetValue(id, out var definition))
            {
                throw new InvalidOperationException(
                    $"{label} catalog does not contain ID '{id}'.");
            }

            return definition;
        }
    }
}

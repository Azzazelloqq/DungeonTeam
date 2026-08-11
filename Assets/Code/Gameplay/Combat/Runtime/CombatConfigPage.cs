using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Combat.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Combat.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Combat Config",
        fileName = "CombatConfig")]
    public sealed class CombatConfigPage : ConfigPage
    {
        [SerializeField]
        private AttackDefinitionConfig[] _attacks = Array.Empty<AttackDefinitionConfig>();

        [SerializeField]
        private CombatLoadoutDefinitionConfig[] _loadouts =
            Array.Empty<CombatLoadoutDefinitionConfig>();

        public CombatCatalog CreateCatalog()
        {
            return new CombatCatalog(_attacks, _loadouts);
        }
    }

    [Serializable]
    public sealed class AttackRankDefinitionConfig
    {
        [SerializeField, Min(1)]
        private int _rank = 1;

        [SerializeField, Min(1)]
        private int _damage = 1;

        [SerializeField, Min(0.1f)]
        private float _range = 1f;

        [SerializeField, Min(0.01f)]
        private float _cooldown = 1f;

        public AttackRankDefinitionConfig(
            int rank,
            int damage,
            float range,
            float cooldown)
        {
            _rank = rank;
            _damage = damage;
            _range = range;
            _cooldown = cooldown;
        }

        internal AttackRankDefinition ToDomain()
        {
            return new AttackRankDefinition(_rank, _damage, _range, _cooldown);
        }
    }

    [Serializable]
    public sealed class AttackDefinitionConfig
    {
        [SerializeField]
        private string _attackId;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private AttackRankDefinitionConfig[] _ranks =
            Array.Empty<AttackRankDefinitionConfig>();

        public AttackDefinitionConfig(
            string attackId,
            string displayName,
            AttackRankDefinitionConfig[] ranks)
        {
            _attackId = attackId;
            _displayName = displayName;
            _ranks = ranks;
        }

        internal AttackDefinition ToDomain(int index)
        {
            if (_ranks == null)
            {
                throw new ArgumentException(
                    $"Attack definition at index {index} has no ranks.");
            }

            var ranks = new AttackRankDefinition[_ranks.Length];
            for (var rankIndex = 0; rankIndex < _ranks.Length; rankIndex++)
            {
                var rank = _ranks[rankIndex] ?? throw new ArgumentException(
                    $"Attack definition at index {index} has a missing rank at index " +
                    $"{rankIndex}.");
                ranks[rankIndex] = rank.ToDomain();
            }

            return new AttackDefinition(_attackId, _displayName, ranks);
        }
    }

    [Serializable]
    public sealed class CombatLoadoutDefinitionConfig
    {
        [SerializeField]
        private string _loadoutId;

        [SerializeField]
        private string _primaryAttackId;

        public CombatLoadoutDefinitionConfig(string loadoutId, string primaryAttackId)
        {
            _loadoutId = loadoutId;
            _primaryAttackId = primaryAttackId;
        }

        internal CombatLoadoutDefinition ToDomain()
        {
            return new CombatLoadoutDefinition(_loadoutId, _primaryAttackId);
        }
    }
}

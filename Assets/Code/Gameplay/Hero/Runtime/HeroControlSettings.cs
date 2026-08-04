using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    [Serializable]
    public sealed class HeroControlSettings
    {
        [SerializeField, Min(0.1f)]
        private float _attackRange = 1.5f;

        [SerializeField, Min(1)]
        private int _attackDamage = 20;

        [SerializeField, Min(0.01f)]
        private float _attackCooldown = 0.8f;

        [SerializeField, Min(1f)]
        private float _targetSelectionRadius = 90f;

        [SerializeField, Min(0f)]
        private float _eyeHeight = 1f;

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        internal float AttackRange => _attackRange;
        internal int AttackDamage => _attackDamage;
        internal float AttackCooldown => _attackCooldown;
        internal float TargetSelectionRadius => _targetSelectionRadius;
        internal float EyeHeight => _eyeHeight;
        internal LayerMask ObstacleMask => _obstacleMask;

        internal void Validate()
        {
            if (_attackRange <= 0f ||
                _attackDamage <= 0 ||
                _attackCooldown <= 0f ||
                _targetSelectionRadius <= 0f ||
                _eyeHeight < 0f)
            {
                throw new InvalidOperationException("Hero control settings are invalid.");
            }
        }
    }
}

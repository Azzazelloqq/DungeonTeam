using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.EnemyAI.Runtime
{
    [Serializable]
    public sealed class EnemyAiSettings
    {
        [SerializeField, Min(0.1f)]
        private float _viewDistance = 8f;

        [SerializeField, Range(1f, 180f)]
        private float _viewAngle = 90f;

        [SerializeField, Min(0.1f)]
        private float _targetLossDistance = 12f;

        [SerializeField, Min(0.1f)]
        private float _attackRange = 1.5f;

        [SerializeField, Min(1)]
        private int _attackDamage = 15;

        [SerializeField, Min(0.01f)]
        private float _attackCooldown = 1f;

        [SerializeField, Min(0f)]
        private float _homeArrivalDistance = 0.25f;

        [SerializeField, Min(0f)]
        private float _eyeHeight = 1f;

        [SerializeField, Min(0f)]
        private float _visionAreaHeight = 0.04f;

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        [SerializeField]
        private Color _idleVisionColor = new(1f, 0.8f, 0.1f, 0.2f);

        [SerializeField]
        private Color _alertVisionColor = new(1f, 0.15f, 0.1f, 0.3f);

        public EnemyAiSettings()
        {
        }

        public EnemyAiSettings(
            float viewDistance,
            float viewAngle,
            float targetLossDistance,
            float attackRange,
            int attackDamage,
            float attackCooldown,
            float homeArrivalDistance = 0.25f,
            float eyeHeight = 1f,
            float visionAreaHeight = 0.04f,
            int obstacleMask = ~0)
        {
            _viewDistance = viewDistance;
            _viewAngle = viewAngle;
            _targetLossDistance = targetLossDistance;
            _attackRange = attackRange;
            _attackDamage = attackDamage;
            _attackCooldown = attackCooldown;
            _homeArrivalDistance = homeArrivalDistance;
            _eyeHeight = eyeHeight;
            _visionAreaHeight = visionAreaHeight;
            _obstacleMask = obstacleMask;
            Validate();
        }

        public float AttackRange => _attackRange;

        internal float ViewDistance => _viewDistance;
        internal float ViewAngle => _viewAngle;
        internal float TargetLossDistance => _targetLossDistance;
        internal int AttackDamage => _attackDamage;
        internal float AttackCooldown => _attackCooldown;
        internal float HomeArrivalDistance => _homeArrivalDistance;
        internal float EyeHeight => _eyeHeight;
        internal float VisionAreaHeight => _visionAreaHeight;
        internal int ObstacleMask => _obstacleMask.value;
        internal Color IdleVisionColor => _idleVisionColor;
        internal Color AlertVisionColor => _alertVisionColor;

        internal void Validate()
        {
            if (_viewDistance <= 0f)
            {
                throw new InvalidOperationException("Enemy AI requires positive view distance.");
            }

            if (_viewAngle is <= 0f or > 180f)
            {
                throw new InvalidOperationException(
                    "Enemy AI view angle must be between 0 and 180 degrees.");
            }

            if (_attackRange <= 0f || _targetLossDistance <= _attackRange)
            {
                throw new InvalidOperationException(
                    "Enemy AI target loss distance must exceed attack range.");
            }

            if (_attackDamage <= 0 || _attackCooldown <= 0f)
            {
                throw new InvalidOperationException(
                    "Enemy AI attack damage and cooldown must be positive.");
            }

            if (_targetLossDistance <= _viewDistance)
            {
                throw new InvalidOperationException(
                    "Enemy AI target loss distance must exceed view distance.");
            }

            if (_homeArrivalDistance < 0f ||
                _eyeHeight < 0f ||
                _visionAreaHeight < 0f)
            {
                throw new InvalidOperationException(
                    "Enemy AI distance and height settings cannot be negative.");
            }
        }
    }
}

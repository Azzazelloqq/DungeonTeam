using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    [Serializable]
    public sealed class HeroControlSettings
    {
        [SerializeField, Min(1f)]
        private float _targetSelectionRadius = 90f;

        [SerializeField, Min(0f)]
        private float _eyeHeight = 1f;

        [SerializeField, Min(1f)]
        private float _autoTargetRange = 8f;

        [SerializeField, Min(1f)]
        private float _manualTargetLossDistance = 10f;

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        internal float TargetSelectionRadius => _targetSelectionRadius;
        internal float EyeHeight => _eyeHeight;
        internal float AutoTargetRange => _autoTargetRange;
        internal float ManualTargetLossDistance => _manualTargetLossDistance;
        internal LayerMask ObstacleMask => _obstacleMask;

        internal void Validate()
        {
            if (_targetSelectionRadius <= 0f ||
                _eyeHeight < 0f ||
                _autoTargetRange <= 0f ||
                _manualTargetLossDistance <= _autoTargetRange)
            {
                throw new InvalidOperationException("Hero control settings are invalid.");
            }
        }
    }
}

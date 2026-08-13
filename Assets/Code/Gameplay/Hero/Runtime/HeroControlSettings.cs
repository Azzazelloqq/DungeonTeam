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

        [SerializeField, Min(0.01f)]
        private float _targetScanInterval = 0.1f;

        [SerializeField, Min(0.01f)]
        private float _manualTargetUnreachableGraceDuration = 0.3f;

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        public HeroControlSettings()
        {
        }

        public HeroControlSettings(
            float targetSelectionRadius,
            float eyeHeight,
            float autoTargetRange,
            float manualTargetLossDistance,
            float targetScanInterval = 0.1f,
            float manualTargetUnreachableGraceDuration = 0.3f,
            int obstacleMask = ~0)
        {
            _targetSelectionRadius = targetSelectionRadius;
            _eyeHeight = eyeHeight;
            _autoTargetRange = autoTargetRange;
            _manualTargetLossDistance = manualTargetLossDistance;
            _targetScanInterval = targetScanInterval;
            _manualTargetUnreachableGraceDuration =
                manualTargetUnreachableGraceDuration;
            _obstacleMask = obstacleMask;
            Validate();
        }

        internal float TargetSelectionRadius => _targetSelectionRadius;
        internal float EyeHeight => _eyeHeight;
        internal float AutoTargetRange => _autoTargetRange;
        internal float ManualTargetLossDistance => _manualTargetLossDistance;
        internal float TargetScanInterval => _targetScanInterval;
        internal float ManualTargetUnreachableGraceDuration =>
            _manualTargetUnreachableGraceDuration;
        internal LayerMask ObstacleMask => _obstacleMask;

        internal void Validate()
        {
            if (_targetSelectionRadius <= 0f ||
                _eyeHeight < 0f ||
                _autoTargetRange <= 0f ||
                _manualTargetLossDistance <= _autoTargetRange ||
                _targetScanInterval <= 0f ||
                _manualTargetUnreachableGraceDuration <= 0f)
            {
                throw new InvalidOperationException("Hero control settings are invalid.");
            }
        }
    }
}

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

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        internal float TargetSelectionRadius => _targetSelectionRadius;
        internal float EyeHeight => _eyeHeight;
        internal LayerMask ObstacleMask => _obstacleMask;

        internal void Validate()
        {
            if (_targetSelectionRadius <= 0f ||
                _eyeHeight < 0f)
            {
                throw new InvalidOperationException("Hero control settings are invalid.");
            }
        }
    }
}

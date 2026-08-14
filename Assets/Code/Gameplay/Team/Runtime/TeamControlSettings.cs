using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    [Serializable]
    public sealed class TeamControlSettings
    {
        [SerializeField, Min(0.1f)]
        private float _startFollowingDistance = 3f;

        [SerializeField, Min(0f)]
        private float _stopFollowingDistance = 1.5f;

        [SerializeField, Min(0.1f)]
        private float _commandRange = 10f;

        [SerializeField, Range(1f, 180f)]
        private float _commandViewAngle = 180f;

        [SerializeField, Min(0f)]
        private float _commandEyeHeight = 1f;

        [SerializeField]
        private LayerMask _obstacleMask = ~0;

        [SerializeField, Min(0.1f)]
        private float _companionTargetLossDistance = 12f;

        [SerializeField, Range(0.01f, 0.99f)]
        private float _companionHealHealthRatio = 0.6f;

        internal float StartFollowingDistance => _startFollowingDistance;
        internal float StopFollowingDistance => _stopFollowingDistance;
        internal float CommandRange => _commandRange;
        internal float CommandViewAngle => _commandViewAngle;
        internal float CommandEyeHeight => _commandEyeHeight;
        internal LayerMask ObstacleMask => _obstacleMask;
        internal float CompanionTargetLossDistance => _companionTargetLossDistance;
        internal float CompanionHealHealthRatio => _companionHealHealthRatio;

        internal void Validate()
        {
            if (_stopFollowingDistance < 0f ||
                _startFollowingDistance <= _stopFollowingDistance)
            {
                throw new InvalidOperationException(
                    "Team Control requires start following distance to exceed stop distance.");
            }

            if (_commandRange <= 0f ||
                _commandViewAngle is < 1f or > 180f ||
                _commandEyeHeight < 0f)
            {
                throw new InvalidOperationException(
                    "Team Control command visibility settings are invalid.");
            }

            if (_companionTargetLossDistance <= 0f ||
                _companionHealHealthRatio is <= 0f or >= 1f)
            {
                throw new InvalidOperationException(
                    "Team Control companion combat settings are invalid.");
            }
        }
    }
}

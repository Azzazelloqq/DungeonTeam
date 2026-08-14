using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    [Serializable]
    public sealed class DungeonRunCameraSettings
    {
        [SerializeField, Min(1f)]
        private float _distance = 10f;

        [SerializeField, Range(15f, 80f)]
        private float _pitch = 55f;

        [SerializeField]
        private float _fallbackYaw = 45f;

        [SerializeField, Min(0f)]
        private float _targetHeight = 1f;

        [SerializeField, Min(0.1f)]
        private float _followSharpness = 10f;

        public DungeonRunCameraSettings()
        {
        }

        internal DungeonRunCameraSettings(
            float distance,
            float pitch,
            float fallbackYaw,
            float targetHeight,
            float followSharpness)
        {
            _distance = distance;
            _pitch = pitch;
            _fallbackYaw = fallbackYaw;
            _targetHeight = targetHeight;
            _followSharpness = followSharpness;
            Validate();
        }

        internal float Distance => _distance;
        internal float Pitch => _pitch;
        internal float FallbackYaw => _fallbackYaw;
        internal float TargetHeight => _targetHeight;
        internal float FollowSharpness => _followSharpness;

        internal void Validate()
        {
            if (_distance <= 0f || _followSharpness <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run camera distance and follow sharpness must be positive.");
            }

            if (_pitch is < 15f or > 80f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run camera pitch must be between 15 and 80 degrees.");
            }

            if (_targetHeight < 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run camera target height cannot be negative.");
            }
        }
    }
}

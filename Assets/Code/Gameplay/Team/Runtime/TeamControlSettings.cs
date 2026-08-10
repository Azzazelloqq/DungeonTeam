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

        [SerializeField, Min(1f)]
        private float _cameraDistance = 10f;

        [SerializeField, Range(15f, 80f)]
        private float _cameraPitch = 55f;

        [SerializeField]
        private float _cameraInitialYaw = 45f;

        [SerializeField, Min(0f)]
        private float _cameraTargetHeight = 1f;

        [SerializeField, Min(0.1f)]
        private float _cameraFollowSharpness = 10f;

        [SerializeField, Min(0.001f)]
        private float _mouseYawSensitivity = 0.15f;

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

        internal float StartFollowingDistance => _startFollowingDistance;
        internal float StopFollowingDistance => _stopFollowingDistance;
        internal float CameraDistance => _cameraDistance;
        internal float CameraPitch => _cameraPitch;
        internal float CameraInitialYaw => _cameraInitialYaw;
        internal float CameraTargetHeight => _cameraTargetHeight;
        internal float CameraFollowSharpness => _cameraFollowSharpness;
        internal float MouseYawSensitivity => _mouseYawSensitivity;
        internal float CommandRange => _commandRange;
        internal float CommandViewAngle => _commandViewAngle;
        internal float CommandEyeHeight => _commandEyeHeight;
        internal LayerMask ObstacleMask => _obstacleMask;
        internal float CompanionTargetLossDistance => _companionTargetLossDistance;

        internal void Validate()
        {
            if (_stopFollowingDistance < 0f ||
                _startFollowingDistance <= _stopFollowingDistance)
            {
                throw new InvalidOperationException(
                    "Team Control requires start following distance to exceed stop distance.");
            }

            if (_cameraDistance <= 0f ||
                _cameraFollowSharpness <= 0f ||
                _mouseYawSensitivity <= 0f)
            {
                throw new InvalidOperationException(
                    "Team Control camera settings require positive values.");
            }

            if (_cameraPitch is < 15f or > 80f)
            {
                throw new InvalidOperationException(
                    "Team Control camera pitch must be between 15 and 80 degrees.");
            }

            if (_commandRange <= 0f ||
                _commandViewAngle is < 1f or > 180f ||
                _commandEyeHeight < 0f)
            {
                throw new InvalidOperationException(
                    "Team Control command visibility settings are invalid.");
            }

            if (_companionTargetLossDistance <= 0f)
            {
                throw new InvalidOperationException(
                    "Team Control companion combat settings are invalid.");
            }
        }
    }
}

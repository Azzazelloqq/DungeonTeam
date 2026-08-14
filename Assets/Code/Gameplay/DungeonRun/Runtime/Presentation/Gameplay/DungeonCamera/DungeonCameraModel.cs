using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera
{
    internal sealed class DungeonCameraModel : DungeonCameraModelBase
    {
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        private readonly DungeonRunCameraSettings _settings;
        private readonly Vector3[] _routeCheckpoints;
        private readonly CameraShotState[] _cameraShots;

        private DungeonCameraPose _currentPose;
        private bool _hasCurrentPose;

        public DungeonCameraModel(
            DungeonRunCameraSettings settings,
            DungeonSpatialLayout spatialLayout)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();
            if (spatialLayout == null)
            {
                throw new ArgumentNullException(nameof(spatialLayout));
            }

            _routeCheckpoints = new Vector3[spatialLayout.RouteCheckpoints.Count];
            for (var index = 0; index < _routeCheckpoints.Length; index++)
            {
                _routeCheckpoints[index] = ToPosition(spatialLayout.RouteCheckpoints[index]);
            }

            ValidateRoute();
            _cameraShots = new CameraShotState[spatialLayout.CameraShots.Count];
            for (var index = 0; index < _cameraShots.Length; index++)
            {
                var shot = spatialLayout.CameraShots[index];
                _cameraShots[index] = new CameraShotState(
                    ToPosition(shot.Pose),
                    shot.RouteCheckpointIndex,
                    shot.LookAheadDistance,
                    shot.ActivationRange,
                    shot.BlendRange);
            }
        }

        public override DungeonCameraPose Snap(Vector3 leaderPosition)
        {
            _currentPose = CalculateDesiredPose(leaderPosition);
            _hasCurrentPose = true;
            return _currentPose;
        }

        public override DungeonCameraPose Advance(Vector3 leaderPosition, float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (!_hasCurrentPose)
            {
                return Snap(leaderPosition);
            }

            var desiredPose = CalculateDesiredPose(leaderPosition);
            var blend = 1f - Mathf.Exp(-_settings.FollowSharpness * deltaTime);
            _currentPose = new DungeonCameraPose(
                Vector3.Lerp(_currentPose.Position, desiredPose.Position, blend),
                Quaternion.Slerp(_currentPose.Rotation, desiredPose.Rotation, blend));
            return _currentPose;
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        private DungeonCameraPose CalculateDesiredPose(Vector3 leaderPosition)
        {
            var target = leaderPosition + Vector3.up * _settings.TargetHeight;
            if (_routeCheckpoints.Length < 2)
            {
                return CreateFollowPose(target, _settings.FallbackYaw);
            }

            var segmentIndex = FindNearestSegment(leaderPosition);
            var routeDirection = GetSegmentDirection(segmentIndex);
            var activeShotIndex = FindActiveShot(leaderPosition, out var shotWeight);
            if (activeShotIndex >= 0)
            {
                routeDirection = BlendTurnDirection(
                    routeDirection,
                    segmentIndex,
                    _cameraShots[activeShotIndex],
                    shotWeight);
            }

            var yaw = Mathf.Atan2(routeDirection.x, routeDirection.z) * Mathf.Rad2Deg;
            var followPose = CreateFollowPose(target, yaw);
            if (activeShotIndex < 0 || shotWeight <= 0f)
            {
                return followPose;
            }

            var shot = _cameraShots[activeShotIndex];
            var shotTarget = target + routeDirection * shot.LookAheadDistance;
            var lookDirection = shotTarget - shot.AnchorPosition;
            var shotRotation = lookDirection.sqrMagnitude > MinimumDirectionSqrMagnitude
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : followPose.Rotation;
            return new DungeonCameraPose(
                Vector3.Lerp(followPose.Position, shot.AnchorPosition, shotWeight),
                Quaternion.Slerp(followPose.Rotation, shotRotation, shotWeight));
        }

        private DungeonCameraPose CreateFollowPose(Vector3 target, float yaw)
        {
            var rotation = Quaternion.Euler(_settings.Pitch, yaw, 0f);
            var position = target + rotation * Vector3.back * _settings.Distance;
            return new DungeonCameraPose(position, rotation);
        }

        private int FindNearestSegment(Vector3 position)
        {
            var nearestSegmentIndex = 0;
            var nearestDistanceSqr = float.MaxValue;
            for (var index = 0; index < _routeCheckpoints.Length - 1; index++)
            {
                var distanceSqr = PlanarDistanceToSegmentSquared(
                    position,
                    _routeCheckpoints[index],
                    _routeCheckpoints[index + 1]);
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestSegmentIndex = index;
                }
            }

            return nearestSegmentIndex;
        }

        private int FindActiveShot(Vector3 leaderPosition, out float weight)
        {
            var selectedIndex = -1;
            weight = 0f;
            for (var index = 0; index < _cameraShots.Length; index++)
            {
                var shot = _cameraShots[index];
                var checkpoint = _routeCheckpoints[shot.RouteCheckpointIndex];
                var distance = PlanarDistance(leaderPosition, checkpoint);
                var candidateWeight = CalculateShotWeight(
                    distance,
                    shot.ActivationRange,
                    shot.BlendRange);
                if (candidateWeight > weight)
                {
                    weight = candidateWeight;
                    selectedIndex = index;
                }
            }

            return selectedIndex;
        }

        private Vector3 BlendTurnDirection(
            Vector3 currentDirection,
            int segmentIndex,
            CameraShotState shot,
            float weight)
        {
            var checkpointIndex = shot.RouteCheckpointIndex;
            if (checkpointIndex <= 0 || checkpointIndex >= _routeCheckpoints.Length - 1)
            {
                return currentDirection;
            }

            var incoming = GetSegmentDirection(checkpointIndex - 1);
            var outgoing = GetSegmentDirection(checkpointIndex);
            var turnProgress = segmentIndex < checkpointIndex
                ? weight * 0.5f
                : 1f - weight * 0.5f;
            var blended = Vector3.Lerp(incoming, outgoing, turnProgress);
            return blended.sqrMagnitude > MinimumDirectionSqrMagnitude
                ? blended.normalized
                : currentDirection;
        }

        private Vector3 GetSegmentDirection(int segmentIndex)
        {
            var direction = _routeCheckpoints[segmentIndex + 1] -
                            _routeCheckpoints[segmentIndex];
            direction.y = 0f;
            return direction.normalized;
        }

        private void ValidateRoute()
        {
            for (var index = 0; index < _routeCheckpoints.Length - 1; index++)
            {
                var direction = _routeCheckpoints[index + 1] - _routeCheckpoints[index];
                direction.y = 0f;
                if (direction.sqrMagnitude <= MinimumDirectionSqrMagnitude)
                {
                    throw new ArgumentException(
                        $"Dungeon camera route segment {index} has no planar length.");
                }
            }
        }

        private static float CalculateShotWeight(
            float distance,
            float activationRange,
            float blendRange)
        {
            if (distance > activationRange)
            {
                return 0f;
            }

            if (blendRange <= 0f)
            {
                return 1f;
            }

            var innerRange = activationRange - blendRange;
            var normalized = Mathf.Clamp01((activationRange - distance) / blendRange);
            if (distance <= innerRange)
            {
                normalized = 1f;
            }

            return normalized * normalized * (3f - 2f * normalized);
        }

        private static float PlanarDistanceToSegmentSquared(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            point.y = 0f;
            start.y = 0f;
            end.y = 0f;
            var segment = end - start;
            var normalizedDistance = Mathf.Clamp01(
                Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            var nearest = start + segment * normalizedDistance;
            return (point - nearest).sqrMagnitude;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.magnitude;
        }

        private static Vector3 ToPosition(DungeonPose pose)
        {
            return new Vector3(pose.PositionX, pose.PositionY, pose.PositionZ);
        }

        private readonly struct CameraShotState
        {
            public CameraShotState(
                Vector3 anchorPosition,
                int routeCheckpointIndex,
                float lookAheadDistance,
                float activationRange,
                float blendRange)
            {
                AnchorPosition = anchorPosition;
                RouteCheckpointIndex = routeCheckpointIndex;
                LookAheadDistance = lookAheadDistance;
                ActivationRange = activationRange;
                BlendRange = blendRange;
            }

            public Vector3 AnchorPosition { get; }
            public int RouteCheckpointIndex { get; }
            public float LookAheadDistance { get; }
            public float ActivationRange { get; }
            public float BlendRange { get; }
        }
    }
}

using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Team.Domain;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    public sealed class TeamController : IDisposable
    {
        private const float MovementThreshold = 0.0001f;
        private const float CompanionDestinationUpdateDistance = 0.5f;

        private readonly ActorInstance _leader;
        private readonly ActorInstance _companion;
        private readonly Camera _camera;
        private readonly ITickHandler _tickHandler;
        private readonly ITeamInput _input;
        private readonly TeamControlSettings _settings;
        private readonly CompanionFollowBrain _companionBrain;

        private Vector3 _lastCompanionDestination;
        private float _cameraYaw;
        private bool _hasCompanionDestination;
        private bool _leaderIsMoving;
        private bool _companionIsMoving;
        private bool _isInitialized;
        private bool _isDisposed;

        public TeamController(
            ActorInstance leader,
            ActorInstance companion,
            Camera camera,
            ITickHandler tickHandler,
            ITeamInput input,
            TeamControlSettings settings)
        {
            _leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _companion = companion ?? throw new ArgumentNullException(nameof(companion));
            _camera = camera != null
                ? camera
                : throw new ArgumentNullException(nameof(camera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _companionBrain = new CompanionFollowBrain(
                _settings.StartFollowingDistance,
                _settings.StopFollowingDistance);
            _cameraYaw = _settings.CameraInitialYaw;
        }

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TeamController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException("Team Controller is already initialized.");
            }

            _input.Enable();
            PositionCamera(immediate: true, deltaTime: 0f);
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _tickHandler.SubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameLateUpdate(OnFrameLateUpdate);
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _leader.StopMovement();
                _companion.StopMovement();
            }

            _input.Dispose();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            UpdateLeaderMovement();
            UpdateCompanionMovement();
        }

        private void OnFrameLateUpdate(float deltaTime)
        {
            _cameraYaw += _input.CameraYawDelta * _settings.MouseYawSensitivity;
            PositionCamera(immediate: false, deltaTime);
        }

        private void UpdateLeaderMovement()
        {
            var movement = _input.Movement;
            if (!_leader.IsAlive || movement.sqrMagnitude <= MovementThreshold)
            {
                if (_leaderIsMoving)
                {
                    _leader.StopMovement();
                    _leaderIsMoving = false;
                }

                return;
            }

            var cameraForward = _camera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            var cameraRight = _camera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            var direction = cameraForward * movement.y + cameraRight * movement.x;
            _leaderIsMoving = _leader.SetMoveDirection(
                Vector3.ClampMagnitude(direction, 1f));
        }

        private void UpdateCompanionMovement()
        {
            if (!_leader.IsAlive || !_companion.IsAlive)
            {
                StopCompanion();
                return;
            }

            var leaderPosition = _leader.Position;
            var distance = Vector3.Distance(_companion.Position, leaderPosition);
            var state = _companionBrain.Evaluate(distance);
            if (state == CompanionFollowState.Holding)
            {
                StopCompanion();
                return;
            }

            if (_hasCompanionDestination &&
                (leaderPosition - _lastCompanionDestination).sqrMagnitude <
                CompanionDestinationUpdateDistance * CompanionDestinationUpdateDistance)
            {
                return;
            }

            if (_companion.TryMoveTo(leaderPosition))
            {
                _lastCompanionDestination = leaderPosition;
                _hasCompanionDestination = true;
                _companionIsMoving = true;
            }
        }

        private void StopCompanion()
        {
            _hasCompanionDestination = false;
            if (!_companionIsMoving)
            {
                return;
            }

            _companion.StopMovement();
            _companionIsMoving = false;
        }

        private void PositionCamera(bool immediate, float deltaTime)
        {
            var target = _leader.Position + Vector3.up * _settings.CameraTargetHeight;
            var targetRotation = Quaternion.Euler(
                _settings.CameraPitch,
                _cameraYaw,
                0f);
            var targetPosition = target + targetRotation * Vector3.back * _settings.CameraDistance;

            if (immediate)
            {
                _camera.transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            var blend = 1f - Mathf.Exp(-_settings.CameraFollowSharpness * deltaTime);
            _camera.transform.SetPositionAndRotation(
                Vector3.Lerp(_camera.transform.position, targetPosition, blend),
                Quaternion.Slerp(_camera.transform.rotation, targetRotation, blend));
        }
    }
}

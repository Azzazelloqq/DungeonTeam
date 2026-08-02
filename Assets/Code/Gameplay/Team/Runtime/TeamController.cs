using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Domain;
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
        private readonly IReadOnlyList<ActorInstance> _enemies;
        private readonly Camera _camera;
        private readonly ITickHandler _tickHandler;
        private readonly ITeamInput _input;
        private readonly TeamControlSettings _settings;
        private readonly CompanionFollowBrain _companionBrain;
        private readonly CompanionCombatBrain _combatBrain;
        private readonly AttackCooldown _attackCooldown;
        private readonly float _minimumCommandViewDot;

        private Vector3 _lastCompanionDestination;
        private ActorInstance _availableAttackTarget;
        private ActorInstance _combatTarget;
        private ActorInstance _highlightedTarget;
        private float _cameraYaw;
        private bool _hasCompanionDestination;
        private bool _leaderIsMoving;
        private bool _companionIsMoving;
        private bool _isForcedFollow;
        private bool _lastCanOrderAttack;
        private bool _lastCanOrderFollow;
        private bool _isInitialized;
        private bool _isDisposed;

        public event Action CommandsChanged;

        public TeamController(
            ActorInstance leader,
            ActorInstance companion,
            IReadOnlyList<ActorInstance> enemies,
            Camera camera,
            ITickHandler tickHandler,
            ITeamInput input,
            TeamControlSettings settings)
        {
            _leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _companion = companion ?? throw new ArgumentNullException(nameof(companion));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
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
            _combatBrain = new CompanionCombatBrain(
                _settings.CompanionAttackRange,
                _settings.CompanionTargetLossDistance);
            _attackCooldown = new AttackCooldown(_settings.CompanionAttackCooldown);
            _minimumCommandViewDot = Mathf.Cos(
                _settings.CommandViewAngle * 0.5f * Mathf.Deg2Rad);
            _cameraYaw = _settings.CameraInitialYaw;
        }

        public bool CanOrderAttack =>
            _companion.IsAlive && _combatTarget == null && _availableAttackTarget != null;

        public bool CanOrderFollow =>
            !_isForcedFollow && (_combatTarget != null || _availableAttackTarget != null);

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
            _leader.AttackedBy += OnTeamMemberAttacked;
            _companion.AttackedBy += OnTeamMemberAttacked;
            RefreshAvailableAttackTarget();
            RefreshTargetHighlight();
            StoreCommandAvailability();
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
            _leader.AttackedBy -= OnTeamMemberAttacked;
            _companion.AttackedBy -= OnTeamMemberAttacked;
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameLateUpdate(OnFrameLateUpdate);
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _leader.StopMovement();
                _companion.StopMovement();
            }

            SetHighlightedTarget(null);
            _availableAttackTarget = null;
            _combatTarget = null;
            CommandsChanged = null;

            _input.Dispose();
        }

        public bool TryOrderAttack()
        {
            if (!CanOrderAttack ||
                !TryGetVisibleDistanceSqr(_availableAttackTarget, out _))
            {
                RefreshAvailableAttackTarget();
                RefreshTargetHighlight();
                PublishCommandAvailabilityIfChanged();
                return false;
            }

            _combatTarget = _availableAttackTarget;
            _availableAttackTarget = null;
            _isForcedFollow = false;
            RefreshTargetHighlight();
            PublishCommandAvailabilityIfChanged();
            return true;
        }

        public void OrderFollow()
        {
            _isForcedFollow = true;
            ClearCombatTarget();
            RefreshAvailableAttackTarget();
            RefreshTargetHighlight();
            PublishCommandAvailabilityIfChanged();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            UpdateLeaderMovement();
            UpdateCombatTarget();
            RefreshAvailableAttackTarget();
            UpdateCompanionMovement(deltaTime);
            RefreshTargetHighlight();
            PublishCommandAvailabilityIfChanged();
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

        private void UpdateCompanionMovement(float deltaTime)
        {
            if (!_companion.IsAlive)
            {
                _attackCooldown.Tick(deltaTime, canAttack: false);
                StopCompanion();
                return;
            }

            if (_combatTarget != null)
            {
                UpdateCompanionCombat(deltaTime);
                return;
            }

            _attackCooldown.Tick(deltaTime, canAttack: false);
            if (!_leader.IsAlive)
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

            MoveCompanionTo(leaderPosition);
        }

        private void UpdateCompanionCombat(float deltaTime)
        {
            var distance = PlanarDistance(_companion.Position, _combatTarget.Position);
            var state = _combatBrain.Evaluate(
                _combatTarget.IsAlive,
                HasClearLine(_companion, _combatTarget),
                distance);
            var shouldAttack = _attackCooldown.Tick(
                deltaTime,
                state == CompanionCombatState.Attack);

            switch (state)
            {
                case CompanionCombatState.Follow:
                    ClearCombatTarget();
                    StopCompanion();
                    break;
                case CompanionCombatState.Chase:
                    MoveCompanionTo(_combatTarget.Position);
                    break;
                case CompanionCombatState.Attack:
                    StopCompanion();
                    if (shouldAttack)
                    {
                        AttackCombatTarget();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AttackCombatTarget()
        {
            if (_combatTarget == null || !_combatTarget.IsAlive)
            {
                return;
            }

            _companion.PlayAttackFeedback();
            _combatTarget.ApplyDamage(_settings.CompanionAttackDamage, _companion);
            if (!_combatTarget.IsAlive)
            {
                ClearCombatTarget();
            }
        }

        private void MoveCompanionTo(Vector3 destination)
        {
            if (_hasCompanionDestination &&
                PlanarSqrDistance(destination, _lastCompanionDestination) <
                CompanionDestinationUpdateDistance * CompanionDestinationUpdateDistance)
            {
                return;
            }

            if (_companion.TryMoveTo(destination))
            {
                _lastCompanionDestination = destination;
                _hasCompanionDestination = true;
                _companionIsMoving = true;
            }
        }

        private void UpdateCombatTarget()
        {
            if (_combatTarget == null)
            {
                return;
            }

            if (!_companion.IsAlive ||
                !_combatTarget.IsAlive ||
                PlanarDistance(_companion.Position, _combatTarget.Position) >
                _settings.CompanionTargetLossDistance)
            {
                ClearCombatTarget();
            }
        }

        private void RefreshAvailableAttackTarget()
        {
            if (_combatTarget != null || !_companion.IsAlive)
            {
                _availableAttackTarget = null;
                return;
            }

            ActorInstance nearest = null;
            var nearestDistanceSqr = float.MaxValue;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var candidate = _enemies[index];
                if (!TryGetVisibleDistanceSqr(candidate, out var distanceSqr) ||
                    distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSqr = distanceSqr;
            }

            _availableAttackTarget = nearest;
        }

        private bool TryGetVisibleDistanceSqr(
            ActorInstance candidate,
            out float distanceSqr)
        {
            distanceSqr = float.MaxValue;
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            if (PlanarSqrDistance(_companion.Position, candidate.Position) >
                _settings.CompanionTargetLossDistance *
                _settings.CompanionTargetLossDistance)
            {
                return false;
            }

            var isVisible = false;
            if (CanSee(_leader, candidate, out var leaderDistanceSqr))
            {
                distanceSqr = leaderDistanceSqr;
                isVisible = true;
            }

            if (CanSee(_companion, candidate, out var companionDistanceSqr))
            {
                distanceSqr = Mathf.Min(distanceSqr, companionDistanceSqr);
                isVisible = true;
            }

            return isVisible;
        }

        private bool CanSee(
            ActorInstance observer,
            ActorInstance candidate,
            out float distanceSqr)
        {
            distanceSqr = float.MaxValue;
            if (!observer.IsAlive)
            {
                return false;
            }

            var direction = candidate.Position - observer.Position;
            direction.y = 0f;
            distanceSqr = direction.sqrMagnitude;
            if (distanceSqr > _settings.CommandRange * _settings.CommandRange)
            {
                return false;
            }

            if (distanceSqr > MovementThreshold)
            {
                direction.Normalize();
                var forward = observer.Forward;
                forward.y = 0f;
                forward.Normalize();
                if (Vector3.Dot(forward, direction) < _minimumCommandViewDot)
                {
                    return false;
                }
            }

            return HasClearLine(observer, candidate);
        }

        private bool HasClearLine(ActorInstance observer, ActorInstance candidate)
        {
            var eyeOffset = Vector3.up * _settings.CommandEyeHeight;
            return !Physics.Linecast(
                observer.Position + eyeOffset,
                candidate.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void OnTeamMemberAttacked(ActorInstance attacker)
        {
            if (_isForcedFollow ||
                _combatTarget != null ||
                !_companion.IsAlive ||
                attacker == null ||
                !attacker.IsAlive ||
                !IsEnemy(attacker))
            {
                return;
            }

            _combatTarget = attacker;
            _availableAttackTarget = null;
            RefreshTargetHighlight();
            PublishCommandAvailabilityIfChanged();
        }

        private bool IsEnemy(ActorInstance actor)
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                if (ReferenceEquals(_enemies[index], actor))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearCombatTarget()
        {
            _combatTarget = null;
        }

        private void RefreshTargetHighlight()
        {
            SetHighlightedTarget(_combatTarget ?? _availableAttackTarget);
        }

        private void SetHighlightedTarget(ActorInstance target)
        {
            if (ReferenceEquals(_highlightedTarget, target))
            {
                return;
            }

            _highlightedTarget?.SetTargetHighlighted(false);
            _highlightedTarget = target;
            _highlightedTarget?.SetTargetHighlighted(true);
        }

        private void StoreCommandAvailability()
        {
            _lastCanOrderAttack = CanOrderAttack;
            _lastCanOrderFollow = CanOrderFollow;
        }

        private void PublishCommandAvailabilityIfChanged()
        {
            var canOrderAttack = CanOrderAttack;
            var canOrderFollow = CanOrderFollow;
            if (canOrderAttack == _lastCanOrderAttack &&
                canOrderFollow == _lastCanOrderFollow)
            {
                return;
            }

            _lastCanOrderAttack = canOrderAttack;
            _lastCanOrderFollow = canOrderFollow;
            CommandsChanged?.Invoke();
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

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            return Mathf.Sqrt(PlanarSqrDistance(first, second));
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}

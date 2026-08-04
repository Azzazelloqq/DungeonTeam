using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.EnemyAI.Domain;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.EnemyAI.Runtime
{
    public sealed class EnemyAiController : IDisposable
    {
        private const float DestinationUpdateDistance = 0.5f;

        private readonly ActorInstance _enemy;
        private readonly ActorInstance[] _targets;
        private readonly ITickHandler _tickHandler;
        private readonly EnemyAiSettings _settings;
        private readonly EnemyAiBrain _brain;
        private readonly AttackCooldown _attackCooldown;
        private readonly Vector3 _homePosition;
        private readonly float _minimumViewDot;

        private EnemyVisionArea _visionArea;
        private ActorInstance _target;
        private Vector3 _lastDestination;
        private bool _hasDestination;
        private bool _isMoving;
        private bool _wasProvoked;
        private bool _isInitialized;
        private bool _isDisposed;

        public EnemyAiController(
            ActorInstance enemy,
            IReadOnlyList<ActorInstance> targets,
            ITickHandler tickHandler,
            EnemyAiSettings settings)
        {
            _enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (targets.Count == 0)
            {
                throw new ArgumentException(
                    "Enemy AI requires at least one target.",
                    nameof(targets));
            }

            _targets = new ActorInstance[targets.Count];
            for (var index = 0; index < targets.Count; index++)
            {
                _targets[index] = targets[index] ?? throw new ArgumentException(
                    $"Enemy AI target at index {index} is missing.",
                    nameof(targets));
            }

            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _homePosition = _enemy.Position;
            _minimumViewDot = Mathf.Cos(_settings.ViewAngle * 0.5f * Mathf.Deg2Rad);
            _brain = new EnemyAiBrain(
                _settings.AttackRange,
                _settings.TargetLossDistance,
                _settings.HomeArrivalDistance);
            _attackCooldown = new AttackCooldown(_settings.AttackCooldown);
        }

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EnemyAiController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException("Enemy AI Controller is already initialized.");
            }

            _visionArea = new EnemyVisionArea(_settings);
            UpdateVisionArea(deltaTime: 0f);
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _enemy.AttackedBy += OnAttacked;
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
                _enemy.AttackedBy -= OnAttacked;
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _enemy.StopMovement();
            }

            _visionArea?.Dispose();
            _visionArea = null;
            _target = null;
            _wasProvoked = false;
        }

        private void OnFrameUpdate(float deltaTime)
        {
            if (!_enemy.IsAlive)
            {
                StopMovement();
                _visionArea.SetVisible(false);
                return;
            }

            _visionArea.SetVisible(true);
            var targetWasSeen = AcquireTargetIfNeeded();
            var targetWasProvoked = _wasProvoked;
            _wasProvoked = false;
            var hasTarget = _target != null && _target.IsAlive;
            var targetDistance = hasTarget
                ? PlanarDistance(_enemy.Position, _target.Position)
                : 0f;
            var distanceToHome = PlanarDistance(_enemy.Position, _homePosition);
            var canAttackTarget = hasTarget &&
                                  targetDistance <= _settings.AttackRange &&
                                  (targetWasSeen || HasClearLine(_target));

            var state = _brain.Evaluate(
                hasTarget,
                targetWasSeen || targetWasProvoked,
                canAttackTarget,
                targetDistance,
                distanceToHome);
            var shouldAttack = _attackCooldown.Tick(
                deltaTime,
                state == EnemyAiState.Attack);

            switch (state)
            {
                case EnemyAiState.Idle:
                    StopMovement();
                    break;
                case EnemyAiState.Attack:
                    StopMovement();
                    _enemy.TryFaceTowards(_target.Position);
                    if (shouldAttack)
                    {
                        AttackTarget();
                    }

                    break;
                case EnemyAiState.Chase:
                    MoveTo(_target.Position);
                    break;
                case EnemyAiState.Return:
                    _target = null;
                    MoveTo(_homePosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateVisionArea(deltaTime);
        }

        private void OnAttacked(ActorInstance attacker)
        {
            if (!_enemy.IsAlive ||
                attacker == null ||
                !attacker.IsAlive ||
                !IsConfiguredTarget(attacker) ||
                (_target != null && _target.IsAlive) ||
                PlanarSqrDistance(_enemy.Position, attacker.Position) >
                _settings.TargetLossDistance * _settings.TargetLossDistance)
            {
                return;
            }

            _target = attacker;
            _wasProvoked = true;
        }

        private bool AcquireTargetIfNeeded()
        {
            if (_target != null && _target.IsAlive)
            {
                return false;
            }

            _target = null;
            var nearestDistanceSqr = float.MaxValue;
            for (var index = 0; index < _targets.Length; index++)
            {
                var candidate = _targets[index];
                if (!CanSee(candidate))
                {
                    continue;
                }

                var distanceSqr = PlanarSqrDistance(
                    _enemy.Position,
                    candidate.Position);
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                _target = candidate;
            }

            return _target != null;
        }

        private bool IsConfiguredTarget(ActorInstance actor)
        {
            for (var index = 0; index < _targets.Length; index++)
            {
                if (ReferenceEquals(_targets[index], actor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanSee(ActorInstance candidate)
        {
            if (!candidate.IsAlive)
            {
                return false;
            }

            var direction = candidate.Position - _enemy.Position;
            direction.y = 0f;
            var sqrDistance = direction.sqrMagnitude;
            if (sqrDistance > _settings.ViewDistance * _settings.ViewDistance ||
                sqrDistance <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            var forward = _enemy.Forward;
            forward.y = 0f;
            forward.Normalize();
            if (Vector3.Dot(forward, direction) < _minimumViewDot)
            {
                return false;
            }

            var eyeOffset = Vector3.up * _settings.EyeHeight;
            return HasClearLine(candidate, eyeOffset);
        }

        private bool HasClearLine(ActorInstance candidate)
        {
            return HasClearLine(candidate, Vector3.up * _settings.EyeHeight);
        }

        private bool HasClearLine(ActorInstance candidate, Vector3 eyeOffset)
        {
            return !Physics.Linecast(
                _enemy.Position + eyeOffset,
                candidate.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void AttackTarget()
        {
            if (_target == null || !_target.IsAlive)
            {
                return;
            }

            _enemy.PlayAttackFeedback();
            _target.ApplyDamage(_settings.AttackDamage, _enemy);
            if (!_target.IsAlive)
            {
                _target = null;
            }
        }

        private void MoveTo(Vector3 destination)
        {
            if (_hasDestination &&
                PlanarSqrDistance(_lastDestination, destination) <
                DestinationUpdateDistance * DestinationUpdateDistance)
            {
                return;
            }

            if (!_enemy.TryMoveTo(destination))
            {
                return;
            }

            _lastDestination = destination;
            _hasDestination = true;
            _isMoving = true;
        }

        private void StopMovement()
        {
            _hasDestination = false;
            if (!_isMoving)
            {
                return;
            }

            _enemy.StopMovement();
            _isMoving = false;
        }

        private void UpdateVisionArea(float deltaTime)
        {
            _visionArea.UpdatePose(
                _enemy.Position,
                _enemy.Forward,
                _settings.VisionAreaHeight,
                deltaTime);
            _visionArea.SetAlerted(
                _brain.State is EnemyAiState.Chase or EnemyAiState.Attack);
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

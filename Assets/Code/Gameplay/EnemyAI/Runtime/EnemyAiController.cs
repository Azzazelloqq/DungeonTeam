using System;
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
        private readonly ActorInstance _leader;
        private readonly ActorInstance _companion;
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
        private bool _isInitialized;
        private bool _isDisposed;

        public EnemyAiController(
            ActorInstance enemy,
            ActorInstance leader,
            ActorInstance companion,
            ITickHandler tickHandler,
            EnemyAiSettings settings)
        {
            _enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            _leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _companion = companion ?? throw new ArgumentNullException(nameof(companion));
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
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _enemy.StopMovement();
            }

            _visionArea?.Dispose();
            _visionArea = null;
            _target = null;
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
                targetWasSeen,
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

        private bool AcquireTargetIfNeeded()
        {
            if (_target != null && _target.IsAlive)
            {
                return false;
            }

            _target = null;
            var leaderVisible = CanSee(_leader);
            var companionVisible = CanSee(_companion);
            if (!leaderVisible && !companionVisible)
            {
                return false;
            }

            if (!leaderVisible)
            {
                _target = _companion;
            }
            else if (!companionVisible)
            {
                _target = _leader;
            }
            else
            {
                var leaderDistance = PlanarSqrDistance(_enemy.Position, _leader.Position);
                var companionDistance = PlanarSqrDistance(_enemy.Position, _companion.Position);
                _target = leaderDistance <= companionDistance ? _leader : _companion;
            }

            return true;
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

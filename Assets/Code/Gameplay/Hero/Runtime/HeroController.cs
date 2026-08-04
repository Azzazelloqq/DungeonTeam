using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Hero.Domain;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    public sealed class HeroController : IDisposable
    {
        private const float MovementThreshold = 0.0001f;
        private const float DestinationUpdateDistance = 0.5f;

        private readonly ActorInstance _hero;
        private readonly IReadOnlyList<ActorInstance> _enemies;
        private readonly Camera _camera;
        private readonly ITickHandler _tickHandler;
        private readonly IHeroInput _input;
        private readonly HeroControlSettings _settings;
        private readonly HeroBasicAttackBrain _attackBrain;
        private readonly AttackCooldown _attackCooldown;

        private ActorInstance _target;
        private Vector3 _lastDestination;
        private bool _hasDestination;
        private bool _isManualMoving;
        private bool _isInitialized;
        private bool _isDisposed;

        public HeroController(
            ActorInstance hero,
            IReadOnlyList<ActorInstance> enemies,
            Camera camera,
            ITickHandler tickHandler,
            IHeroInput input,
            HeroControlSettings settings)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _attackBrain = new HeroBasicAttackBrain(_settings.AttackRange);
            _attackCooldown = new AttackCooldown(_settings.AttackCooldown);
        }

        public event Action TargetChanged;

        public ActorInstance Target => _target;

        public bool CanAttack => _hero.IsAlive && _target != null && _target.IsAlive;

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(HeroController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException("Hero Controller is already initialized.");
            }

            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
        }

        public bool TrySetTarget(ActorInstance target)
        {
            if (target != null && (!target.IsAlive || !IsEnemy(target)))
            {
                return false;
            }

            SetTarget(target);
            return true;
        }

        public bool TryRequestBasicAttack()
        {
            if (!CanAttack)
            {
                return false;
            }

            _attackBrain.RequestAttack();
            return true;
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
                _hero.StopMovement();
            }

            _attackBrain.Cancel();
            SetTarget(null);
            TargetChanged = null;
        }

        private void OnFrameUpdate(float deltaTime)
        {
            if (!_hero.IsAlive)
            {
                CancelActiveAttack();
                StopHero();
                SetTarget(null);
                return;
            }

            if (_input.TargetSelectionWasPressed)
            {
                SelectTargetAt(_input.PointerPosition);
            }

            var movement = _input.Movement;
            if (movement.sqrMagnitude > MovementThreshold)
            {
                CancelActiveAttack();
                MoveManually(movement);
                _attackCooldown.Tick(deltaTime, canAttack: false);
                return;
            }

            if (_isManualMoving)
            {
                _hero.StopMovement();
                _isManualMoving = false;
            }

            if (_input.BasicAttackWasPressed)
            {
                TryRequestBasicAttack();
            }

            UpdateBasicAttack(deltaTime);
        }

        private void UpdateBasicAttack(float deltaTime)
        {
            if (_target != null && !_target.IsAlive)
            {
                CancelActiveAttack();
                SetTarget(null);
            }

            if (_attackBrain.State == HeroBasicAttackState.Idle)
            {
                _attackCooldown.Tick(deltaTime, canAttack: false);
                StopAutomaticMovement();
                return;
            }

            var hasTarget = _target != null;
            var distance = hasTarget
                ? PlanarDistance(_hero.Position, _target.Position)
                : 0f;
            var state = _attackBrain.Evaluate(
                hasTarget && _target.IsAlive,
                distance,
                hasTarget && HasClearLine(_target));
            var shouldAttack = _attackCooldown.Tick(
                deltaTime,
                state == HeroBasicAttackState.ReadyToAttack);

            switch (state)
            {
                case HeroBasicAttackState.Idle:
                    StopAutomaticMovement();
                    break;
                case HeroBasicAttackState.Approaching:
                    MoveToTarget();
                    break;
                case HeroBasicAttackState.ReadyToAttack:
                    StopAutomaticMovement();
                    _hero.TryFaceTowards(_target.Position);
                    if (shouldAttack && _attackBrain.ConsumeAttack())
                    {
                        AttackTarget();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveManually(Vector2 movement)
        {
            _hasDestination = false;
            var cameraForward = _camera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            var cameraRight = _camera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            var direction = cameraForward * movement.y + cameraRight * movement.x;
            _isManualMoving = _hero.SetMoveDirection(Vector3.ClampMagnitude(direction, 1f));
        }

        private void MoveToTarget()
        {
            if (_target == null)
            {
                return;
            }

            var destination = _target.Position;
            if (_hasDestination &&
                PlanarSqrDistance(destination, _lastDestination) <
                DestinationUpdateDistance * DestinationUpdateDistance)
            {
                return;
            }

            if (_hero.TryMoveTo(destination))
            {
                _lastDestination = destination;
                _hasDestination = true;
            }
        }

        private void AttackTarget()
        {
            if (_target == null || !_target.IsAlive)
            {
                return;
            }

            _hero.PlayAttackFeedback();
            _target.ApplyDamage(_settings.AttackDamage, _hero);
            if (!_target.IsAlive)
            {
                SetTarget(null);
            }
        }

        private void SelectTargetAt(Vector2 screenPosition)
        {
            ActorInstance nearest = null;
            var nearestDistanceSqr = _settings.TargetSelectionRadius *
                                     _settings.TargetSelectionRadius;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var candidate = _enemies[index];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                var targetPoint = candidate.Position + Vector3.up * _settings.EyeHeight;
                var candidateScreenPosition = _camera.WorldToScreenPoint(targetPoint);
                if (candidateScreenPosition.z <= 0f)
                {
                    continue;
                }

                if (Physics.Linecast(
                        _camera.transform.position,
                        targetPoint,
                        _settings.ObstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var difference = (Vector2)candidateScreenPosition - screenPosition;
                var distanceSqr = difference.sqrMagnitude;
                if (distanceSqr > nearestDistanceSqr)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSqr = distanceSqr;
            }

            SetTarget(nearest);
        }

        private bool HasClearLine(ActorInstance target)
        {
            var eyeOffset = Vector3.up * _settings.EyeHeight;
            return !Physics.Linecast(
                _hero.Position + eyeOffset,
                target.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
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

        private void SetTarget(ActorInstance target)
        {
            if (ReferenceEquals(_target, target))
            {
                return;
            }

            CancelActiveAttack();
            _target = target;
            TargetChanged?.Invoke();
        }

        private void CancelActiveAttack()
        {
            _attackBrain.Cancel();
            StopAutomaticMovement();
        }

        private void StopAutomaticMovement()
        {
            if (!_hasDestination)
            {
                return;
            }

            _hero.StopMovement();
            _hasDestination = false;
        }

        private void StopHero()
        {
            if (!_hasDestination && !_isManualMoving)
            {
                return;
            }

            _hero.StopMovement();
            _hasDestination = false;
            _isManualMoving = false;
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

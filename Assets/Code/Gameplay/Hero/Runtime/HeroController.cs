using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Hero.Domain;
using DungeonTeam.Gameplay.Skills.Domain;
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
        private readonly ActorCombatController _combat;

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
            HeroControlSettings settings,
            ActorCombatController combat)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (!ReferenceEquals(_combat.Actor, _hero))
            {
                throw new ArgumentException(
                    "Hero combat controller must belong to the hero.",
                    nameof(combat));
            }
            _settings.Validate();

            _attackBrain = new HeroBasicAttackBrain(_combat.GetRange(SkillSlot.Primary));
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
                _combat.Tick(deltaTime);
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

            _combat.Tick(deltaTime);
            UpdateBasicAttack();
        }

        private void UpdateBasicAttack()
        {
            if (_target != null && !_target.IsAlive)
            {
                CancelActiveAttack();
                SetTarget(null);
            }

            if (_attackBrain.State == HeroBasicAttackState.Idle)
            {
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
                    if (_combat.IsReady(SkillSlot.Primary) && _attackBrain.ConsumeAttack())
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

            var result = _combat.TryUse(
                SkillSlot.Primary,
                _target,
                HasClearLine(_target));
            if (result == SkillUseResult.Executed && !_target.IsAlive)
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
            _combat.CancelActiveUse();
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

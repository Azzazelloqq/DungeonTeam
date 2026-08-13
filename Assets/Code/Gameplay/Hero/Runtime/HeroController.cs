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
        private readonly IReadOnlyList<ActorInstance> _allies;
        private readonly IReadOnlyList<ActorInstance> _enemies;
        private readonly Camera _camera;
        private readonly ITickHandler _tickHandler;
        private readonly IHeroInput _input;
        private readonly HeroControlSettings _settings;
        private readonly HeroSkillActionBrain _skillActionBrain = new();
        private readonly HeroTargetSelectionBrain _targetSelectionBrain;
        private readonly ActorCombatController _combat;
        private readonly SkillSlot? _autoTargetSlot;
        private readonly Func<Vector3, Vector3, bool> _hasCompletePath;

        private ActorInstance _target;
        private SkillSlot _selectedSlot;
        private SkillSlot? _pendingSlot;
        private ActorInstance _pendingTarget;
        private SkillTargetRelation _pendingTargetRelation;
        private Vector3 _lastDestination;
        private bool _hasDestination;
        private bool _isManualMoving;
        private bool _isCurrentTargetReachable;
        private float _targetScanElapsed;
        private bool _isInitialized;
        private bool _isDisposed;

        public HeroController(
            ActorInstance hero,
            IReadOnlyList<ActorInstance> allies,
            IReadOnlyList<ActorInstance> enemies,
            Camera camera,
            ITickHandler tickHandler,
            IHeroInput input,
            HeroControlSettings settings,
            ActorCombatController combat,
            Func<Vector3, Vector3, bool> hasCompletePath)
        {
            _hero = hero ?? throw new ArgumentNullException(nameof(hero));
            _allies = allies ?? throw new ArgumentNullException(nameof(allies));
            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _hasCompletePath = hasCompletePath ?? throw new ArgumentNullException(
                nameof(hasCompletePath));
            if (!ReferenceEquals(_combat.Actor, _hero))
            {
                throw new ArgumentException(
                    "Hero combat controller must belong to the hero.",
                    nameof(combat));
            }
            _settings.Validate();
            _targetSelectionBrain = new HeroTargetSelectionBrain(
                _settings.ManualTargetLossDistance,
                _settings.ManualTargetUnreachableGraceDuration);

            _selectedSlot = _combat.HasSlot(SkillSlot.Primary)
                ? SkillSlot.Primary
                : _combat.Slots[0].Slot;
            _autoTargetSlot = FindEnemyTargetSlot();
        }

        public ActorInstance Target => _target;
        public bool IsTargetManuallySelected =>
            _targetSelectionBrain.Mode == HeroTargetSelectionMode.Manual;
        public SkillSlot SelectedSlot => _selectedSlot;
        public SkillSlot? PendingSlot => _pendingSlot;

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

            RefreshTargetSelection(deltaTime: 0f, forceScan: true);
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
        }

        public bool TrySetTarget(ActorInstance target)
        {
            if (target != null &&
                (!target.IsAlive ||
                 (!IsEnemy(target) && !IsAlly(target)) ||
                 PlanarSqrDistance(_hero.Position, target.Position) >
                 _settings.ManualTargetLossDistance *
                 _settings.ManualTargetLossDistance ||
                 !HasCompletePath(target)))
            {
                return false;
            }

            if (target == null)
            {
                UseAutomaticTargeting();
            }
            else
            {
                _targetSelectionBrain.SelectManual();
                SetTarget(target);
            }

            return true;
        }

        public bool CanRequestSkill(SkillSlot slot)
        {
            return _hero.IsAlive &&
                   !_pendingSlot.HasValue &&
                   _combat.HasSlot(slot) &&
                   _combat.IsReady(slot) &&
                   TryResolveSkillTarget(slot, out _, out _);
        }

        public bool TryRequestSkill(SkillSlot slot)
        {
            if (!_hero.IsAlive || !_combat.HasSlot(slot))
                return false;

            _selectedSlot = slot;
            if (!_combat.IsReady(slot))
                return false;

            AcquireAutoTargetForRequest(slot);

            if (!TryResolveSkillTarget(slot, out var target, out var targetRelation))
                return false;

            if (targetRelation == SkillTargetRelation.Enemy &&
                !ReferenceEquals(_target, target))
            {
                SetTarget(target);
            }

            _pendingSlot = slot;
            _pendingTarget = target;
            _pendingTargetRelation = targetRelation;
            _skillActionBrain.Request(_combat.GetRange(slot));
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

            CancelActiveSkillAction();
            _target = null;
            _targetSelectionBrain.UseAutomatic();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            if (!_hero.IsAlive)
            {
                CancelActiveSkillAction();
                StopHero();
                _targetSelectionBrain.UseAutomatic();
                _target = null;
                return;
            }

            var hasTargetSelection = _input.TryConsumeTargetSelection(out var pointerPosition);
            if (hasTargetSelection && !_pendingSlot.HasValue && !_combat.IsBusy)
            {
                SelectTargetAt(pointerPosition);
            }

            RefreshTargetSelection(deltaTime);
            var hasRequestedSkill = _input.TryConsumeSkillRequest(out var requestedSlot);

            var movement = _input.Movement;
            if (movement.sqrMagnitude > MovementThreshold)
            {
                CancelActiveSkillAction();
                MoveManually(movement);
                _combat.Tick(deltaTime);
                return;
            }

            if (_isManualMoving)
            {
                _hero.StopMovement();
                _isManualMoving = false;
            }

            if (hasRequestedSkill)
            {
                TryRequestSkill(requestedSlot);
            }

            _combat.Tick(deltaTime);
            UpdateSkillAction();
        }

        private void UpdateSkillAction()
        {
            if (_target != null && !_target.IsAlive)
            {
                if (ReferenceEquals(_target, _pendingTarget))
                {
                    SetTarget(null);
                }
                else
                {
                    _target = null;
                }
            }

            if (_skillActionBrain.State == HeroSkillActionState.Idle)
            {
                StopAutomaticMovement();
                return;
            }

            if (!_pendingSlot.HasValue)
            {
                CancelActiveSkillAction();
                return;
            }

            var actionTarget = _pendingTarget;
            var pendingSlot = _pendingSlot.Value;
            if (!_combat.CanTarget(
                    pendingSlot,
                    actionTarget,
                    _pendingTargetRelation))
            {
                CancelActiveSkillAction();
                return;
            }

            var hasTarget = actionTarget != null;
            var distance = hasTarget
                ? PlanarDistance(_hero.Position, actionTarget.Position)
                : 0f;
            var state = _skillActionBrain.Evaluate(
                hasTarget && actionTarget.IsAlive,
                distance,
                hasTarget &&
                (_pendingTargetRelation == SkillTargetRelation.Self ||
                 HasClearLine(actionTarget)));
            switch (state)
            {
                case HeroSkillActionState.Idle:
                    StopAutomaticMovement();
                    break;
                case HeroSkillActionState.Approaching:
                    MoveToTarget(actionTarget);
                    break;
                case HeroSkillActionState.ReadyToUse:
                    StopAutomaticMovement();
                    var slot = pendingSlot;
                    if (!_combat.IsReady(slot))
                    {
                        CancelActiveSkillAction();
                        break;
                    }

                    if (_skillActionBrain.ConsumeUse())
                    {
                        _pendingSlot = null;
                        _pendingTarget = null;
                        UseSkillOnTarget(slot, actionTarget, _pendingTargetRelation);
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

        private void MoveToTarget(ActorInstance target)
        {
            if (target == null)
            {
                return;
            }

            var destination = target.Position;
            if (_hasDestination &&
                PlanarSqrDistance(destination, _lastDestination) <
                DestinationUpdateDistance * DestinationUpdateDistance)
            {
                return;
            }

            if (!_hero.TryMoveTo(destination))
            {
                CancelActiveSkillAction();
                return;
            }

            _lastDestination = destination;
            _hasDestination = true;
        }

        private void UseSkillOnTarget(
            SkillSlot slot,
            ActorInstance target,
            SkillTargetRelation targetRelation)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var result = _combat.TryUse(new SkillUseRequest(
                slot,
                target,
                targetRelation,
                targetRelation == SkillTargetRelation.Self || HasClearLine(target)));
            if (result == SkillUseResult.Executed &&
                ReferenceEquals(_target, target) &&
                !target.IsAlive)
            {
                SetTarget(null);
            }
        }

        private void SelectTargetAt(Vector2 screenPosition)
        {
            ActorInstance nearest = null;
            var nearestIndex = -1;
            var nearestDistanceSqr = _settings.TargetSelectionRadius *
                                     _settings.TargetSelectionRadius;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var candidate = _enemies[index];
                if (candidate == null ||
                    !candidate.IsAlive ||
                    PlanarSqrDistance(_hero.Position, candidate.Position) >
                    _settings.ManualTargetLossDistance *
                    _settings.ManualTargetLossDistance)
                {
                    continue;
                }

                var targetPoint = candidate.Position + Vector3.up * _settings.EyeHeight;
                var candidateScreenPosition = _camera.WorldToScreenPoint(targetPoint);
                if (candidateScreenPosition.z <= 0f)
                {
                    continue;
                }

                var difference = (Vector2)candidateScreenPosition - screenPosition;
                var distanceSqr = difference.sqrMagnitude;
                if (distanceSqr > nearestDistanceSqr ||
                    (distanceSqr == nearestDistanceSqr &&
                     !ShouldPreferCandidate(candidate, index, nearest, nearestIndex)))
                {
                    continue;
                }

                if (Physics.Linecast(
                        _camera.transform.position,
                        targetPoint,
                        _settings.ObstacleMask,
                        QueryTriggerInteraction.Ignore) ||
                    !HasCompletePath(candidate))
                {
                    continue;
                }

                nearest = candidate;
                nearestIndex = index;
                nearestDistanceSqr = distanceSqr;
            }

            if (nearest == null)
            {
                UseAutomaticTargeting();
                return;
            }

            if (_targetSelectionBrain.Mode == HeroTargetSelectionMode.Manual &&
                ReferenceEquals(_target, nearest))
            {
                UseAutomaticTargeting();
                return;
            }

            TrySetTarget(nearest);
        }

        private void RefreshTargetSelection(float deltaTime, bool forceScan = false)
        {
            _targetScanElapsed += deltaTime;
            var shouldScan = forceScan ||
                             _targetScanElapsed >= _settings.TargetScanInterval;
            if (shouldScan)
            {
                _targetScanElapsed = 0f;
            }

            if (_combat.IsBusy)
            {
                return;
            }

            if (_targetSelectionBrain.Mode == HeroTargetSelectionMode.Manual)
            {
                var hasTarget = _target != null &&
                                (IsEnemy(_target) || IsAlly(_target));
                var isTargetAlive = hasTarget && _target.IsAlive;
                var targetDistance = isTargetAlive
                    ? PlanarDistance(_hero.Position, _target.Position)
                    : 0f;
                if (shouldScan && isTargetAlive)
                    _isCurrentTargetReachable = HasCompletePath(_target);

                if (_targetSelectionBrain.Evaluate(
                        hasTarget,
                        isTargetAlive,
                        _isCurrentTargetReachable,
                        targetDistance,
                        deltaTime) ==
                    HeroTargetSelectionMode.Manual)
                {
                    return;
                }

                SetTarget(null);
                shouldScan = true;
            }
            else if (_target != null && !_target.IsAlive)
            {
                SetTarget(null);
                shouldScan = true;
            }

            if (_pendingSlot.HasValue && ReferenceEquals(_target, _pendingTarget))
                return;

            if (!shouldScan)
                return;

            SetTarget(_autoTargetSlot.HasValue
                ? FindAutoTarget(_autoTargetSlot.Value)
                : null);
        }

        private void UseAutomaticTargeting()
        {
            _targetSelectionBrain.UseAutomatic();
            SetTarget(null);
            _targetScanElapsed = _settings.TargetScanInterval;
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

        private bool IsAlly(ActorInstance actor)
        {
            for (var index = 0; index < _allies.Count; index++)
            {
                if (ReferenceEquals(_allies[index], actor))
                    return true;
            }

            return false;
        }

        private bool TryResolveSkillTarget(
            SkillSlot slot,
            out ActorInstance target,
            out SkillTargetRelation targetRelation)
        {
            var targetRule = _combat.GetSlotState(slot).Skill.TargetRule;
            switch (targetRule)
            {
                case SkillTargetRule.EnemyActor:
                    target = _target != null && IsEnemy(_target) && _target.IsAlive
                        ? _target
                        : null;

                    targetRelation = SkillTargetRelation.Enemy;
                    break;
                case SkillTargetRule.AllyOrSelfActor:
                    target = IsAlly(_target) && _target.IsAlive ? _target : _hero;
                    targetRelation = ReferenceEquals(target, _hero)
                        ? SkillTargetRelation.Self
                        : SkillTargetRelation.Ally;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Skill target rule '{targetRule}' is unsupported.");
            }

            return _combat.CanTarget(slot, target, targetRelation);
        }

        private ActorInstance FindAutoTarget(SkillSlot slot)
        {
            ActorInstance nearest = null;
            var nearestIndex = -1;
            var nearestDistanceSqr = _settings.AutoTargetRange *
                                     _settings.AutoTargetRange;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var candidate = _enemies[index];
                if (candidate == null ||
                    !candidate.IsAlive ||
                    !_combat.CanTarget(slot, candidate, SkillTargetRelation.Enemy))
                {
                    continue;
                }

                var distanceSqr = PlanarSqrDistance(_hero.Position, candidate.Position);
                if (distanceSqr > nearestDistanceSqr ||
                    (distanceSqr == nearestDistanceSqr &&
                     !ShouldPreferCandidate(candidate, index, nearest, nearestIndex)) ||
                    !HasClearLine(candidate) ||
                    !HasCompletePath(candidate))
                {
                    continue;
                }

                nearest = candidate;
                nearestIndex = index;
                nearestDistanceSqr = distanceSqr;
            }

            return nearest;
        }

        private SkillSlot? FindEnemyTargetSlot()
        {
            for (var index = 0; index < _combat.Slots.Count; index++)
            {
                var slot = _combat.Slots[index];
                if (slot.Skill.TargetRule == SkillTargetRule.EnemyActor)
                {
                    return slot.Slot;
                }
            }

            return null;
        }

        private void SetTarget(ActorInstance target)
        {
            if (ReferenceEquals(_target, target))
            {
                return;
            }

            if (ReferenceEquals(_target, _pendingTarget))
            {
                CancelActiveSkillAction();
            }

            _target = target;
            _isCurrentTargetReachable = target != null;
        }

        private void AcquireAutoTargetForRequest(SkillSlot slot)
        {
            if (_targetSelectionBrain.Mode != HeroTargetSelectionMode.Automatic ||
                _combat.GetSlotState(slot).Skill.TargetRule != SkillTargetRule.EnemyActor)
            {
                return;
            }

            SetTarget(FindAutoTarget(slot));
            _targetScanElapsed = 0f;
        }

        private bool HasCompletePath(ActorInstance target)
        {
            return _hasCompletePath(_hero.Position, target.Position);
        }

        private static bool ShouldPreferCandidate(
            ActorInstance candidate,
            int candidateIndex,
            ActorInstance current,
            int currentIndex)
        {
            if (current == null)
                return true;

            var idComparison = string.CompareOrdinal(candidate.ActorId, current.ActorId);
            return idComparison < 0 ||
                   (idComparison == 0 && candidateIndex < currentIndex);
        }

        private void CancelActiveSkillAction()
        {
            _skillActionBrain.Cancel();
            _pendingSlot = null;
            _pendingTarget = null;
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

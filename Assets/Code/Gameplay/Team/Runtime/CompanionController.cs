using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Team.Domain;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    internal sealed class CompanionController : IDisposable
    {
        private const float DestinationUpdateDistance = 0.5f;

        private readonly ActorInstance _actor;
        private readonly Vector3 _formationOffset;
        private readonly TeamControlSettings _settings;
        private readonly CompanionFollowBrain _followBrain;
        private readonly CompanionCombatBrain _combatBrain;
        private readonly ActorCombatController _combat;

        private Vector3 _lastDestination;
        private bool _hasDestination;
        private bool _isMoving;
        private bool _isDisposed;

        public CompanionController(
            ActorInstance actor,
            Vector3 formationOffset,
            TeamControlSettings settings,
            ActorCombatController combat)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _formationOffset = formationOffset;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (!ReferenceEquals(_combat.Actor, _actor))
            {
                throw new ArgumentException(
                    "Companion combat controller must belong to the companion.",
                    nameof(combat));
            }
            _followBrain = new CompanionFollowBrain(
                settings.StartFollowingDistance,
                settings.StopFollowingDistance);
            _combatBrain = new CompanionCombatBrain(settings.CompanionTargetLossDistance);
        }

        public ActorInstance Actor => _actor;

        public bool CanCancelPreCommitAction =>
            !_isDisposed && _combat.CanCancelActiveUse;

        public bool Tick(
            float deltaTime,
            ActorInstance leader,
            ActorInstance healTarget,
            ActorInstance attackTarget,
            CompanionCommandMode commandMode,
            Vector3? tacticalAnchor)
        {
            if (_isDisposed)
            {
                return commandMode == CompanionCommandMode.Follow;
            }

            _combat.Tick(deltaTime);
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                _actor.IsAlive,
                _combat.IsBusy,
                commandMode,
                healTarget != null && healTarget.IsAlive,
                attackTarget != null && attackTarget.IsAlive));
            if (decision.Kind == CompanionDecisionKind.Heal &&
                !TryUseHealSkill(healTarget))
            {
                decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                    isActorActive: true,
                    isActionInProgress: false,
                    commandMode: commandMode,
                    hasHealTarget: false,
                    hasAttackTarget: attackTarget != null && attackTarget.IsAlive));
            }

            switch (decision.Kind)
            {
                case CompanionDecisionKind.Hold:
                    Stop();
                    return commandMode == CompanionCommandMode.Follow &&
                           decision.Reason == CompanionDecisionReason.ActorInactive;
                case CompanionDecisionKind.FollowFormation:
                {
                    var isAtFormation = UpdateFollow(
                        leader,
                        commandMode == CompanionCommandMode.Follow
                            ? null
                            : tacticalAnchor);
                    return commandMode == CompanionCommandMode.Follow && isAtFormation;
                }
                case CompanionDecisionKind.Heal:
                    return false;
                case CompanionDecisionKind.Attack:
                    if (!TryUseAttackSkill(attackTarget))
                    {
                        Stop();
                    }

                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision));
            }
        }

        public void CancelPreCommitAction()
        {
            if (_isDisposed)
            {
                return;
            }

            _combat.CancelActiveUse();
            Stop();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Stop();
        }

        private bool UpdateFollow(ActorInstance leader, Vector3? tacticalAnchor)
        {
            if (!leader.IsAlive)
            {
                Stop();
                return true;
            }

            var followPosition = tacticalAnchor ?? GetFollowPosition(leader);
            var state = _followBrain.Evaluate(
                Vector3.Distance(_actor.Position, followPosition));
            if (state == CompanionFollowState.Holding)
            {
                Stop();
                return true;
            }

            MoveTo(followPosition);
            return false;
        }

        private bool TryUseHealSkill(ActorInstance target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var relation = ReferenceEquals(_actor, target)
                ? SkillTargetRelation.Self
                : SkillTargetRelation.Ally;
            var slots = _combat.Slots;
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (!slot.IsReady ||
                    slot.Skill is not DirectHealSkillDefinition ||
                    !_combat.CanTarget(slot.Slot, target, relation))
                {
                    continue;
                }

                return TryUseSkill(slot, target, relation);
            }

            return false;
        }

        private bool TryUseAttackSkill(ActorInstance target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            CombatSkillSlotState? readyChaseSlot = null;
            CombatSkillSlotState? cooldownSlot = null;
            var hasClearLine = HasClearLine(target);
            var distance = PlanarDistance(_actor.Position, target.Position);
            var slots = _combat.Slots;
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (slot.Skill.TargetRule != SkillTargetRule.EnemyActor ||
                    !_combat.CanTarget(slot.Slot, target, SkillTargetRelation.Enemy))
                {
                    continue;
                }

                var state = _combatBrain.Evaluate(
                    hasTarget: true,
                    hasClearLine,
                    distance,
                    slot.Level.Range);
                if (state == CompanionCombatState.Follow)
                {
                    continue;
                }

                if (slot.IsReady)
                {
                    if (state == CompanionCombatState.UseSkill)
                    {
                        Stop();
                        return _combat.TryUse(new SkillUseRequest(
                            slot.Slot,
                            target,
                            SkillTargetRelation.Enemy,
                            hasClearLine)) == SkillUseResult.Executed;
                    }

                    SelectLongerRangeSlot(ref readyChaseSlot, slot);
                    continue;
                }

                SelectLongerRangeSlot(ref cooldownSlot, slot);
            }

            if (readyChaseSlot.HasValue)
            {
                return TryMaintainSkillRange(readyChaseSlot.Value, target);
            }

            return cooldownSlot.HasValue &&
                   TryMaintainSkillRange(cooldownSlot.Value, target);
        }

        private static void SelectLongerRangeSlot(
            ref CombatSkillSlotState? selected,
            CombatSkillSlotState candidate)
        {
            if (!selected.HasValue ||
                candidate.Level.Range > selected.Value.Level.Range)
            {
                selected = candidate;
            }
        }

        private bool TryMaintainSkillRange(
            CombatSkillSlotState slot,
            ActorInstance target)
        {
            var state = _combatBrain.Evaluate(
                target.IsAlive,
                HasClearLine(target),
                PlanarDistance(_actor.Position, target.Position),
                slot.Level.Range);

            switch (state)
            {
                case CompanionCombatState.Follow:
                    return false;
                case CompanionCombatState.Chase:
                    MoveTo(target.Position);
                    return true;
                case CompanionCombatState.UseSkill:
                    Stop();
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryUseSkill(
            CombatSkillSlotState slot,
            ActorInstance target,
            SkillTargetRelation relation)
        {
            var hasClearLine = relation == SkillTargetRelation.Self || HasClearLine(target);
            var state = _combatBrain.Evaluate(
                target.IsAlive,
                hasClearLine,
                PlanarDistance(_actor.Position, target.Position),
                slot.Level.Range);

            switch (state)
            {
                case CompanionCombatState.Follow:
                    return false;
                case CompanionCombatState.Chase:
                    MoveTo(target.Position);
                    return true;
                case CompanionCombatState.UseSkill:
                    Stop();
                    return _combat.TryUse(new SkillUseRequest(
                        slot.Slot,
                        target,
                        relation,
                        hasClearLine)) == SkillUseResult.Executed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 GetFollowPosition(ActorInstance leader)
        {
            var forward = leader.Forward;
            forward.y = 0f;
            var rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
            return leader.Position + rotation * _formationOffset;
        }

        private void MoveTo(Vector3 destination)
        {
            if (_hasDestination &&
                PlanarSqrDistance(destination, _lastDestination) <
                DestinationUpdateDistance * DestinationUpdateDistance)
            {
                return;
            }

            if (_actor.TryMoveTo(destination))
            {
                _lastDestination = destination;
                _hasDestination = true;
                _isMoving = true;
            }
        }

        private void Stop()
        {
            _hasDestination = false;
            if (!_isMoving)
            {
                return;
            }

            _actor.StopMovement();
            _isMoving = false;
        }

        private bool HasClearLine(ActorInstance target)
        {
            var eyeOffset = Vector3.up * _settings.CommandEyeHeight;
            return !Physics.Linecast(
                _actor.Position + eyeOffset,
                target.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
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

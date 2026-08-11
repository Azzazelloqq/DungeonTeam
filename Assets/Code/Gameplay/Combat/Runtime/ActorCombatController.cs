using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using UnityEngine;

namespace DungeonTeam.Gameplay.Combat.Runtime
{
    public enum SkillUseResult
    {
        Executed,
        Busy,
        OnCooldown,
        OutOfRange,
        Blocked,
        InvalidTarget
    }

    public readonly struct SkillUseRequest
    {
        public SkillUseRequest(
            SkillSlot slot,
            ActorInstance target,
            SkillTargetRelation targetRelation,
            bool hasClearLine)
        {
            if (!Enum.IsDefined(typeof(SkillSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (!Enum.IsDefined(typeof(SkillTargetRelation), targetRelation))
                throw new ArgumentOutOfRangeException(nameof(targetRelation));

            Slot = slot;
            Target = target;
            TargetRelation = targetRelation;
            HasClearLine = hasClearLine;
        }

        public SkillSlot Slot { get; }
        public ActorInstance Target { get; }
        public SkillTargetRelation TargetRelation { get; }
        public bool HasClearLine { get; }
    }

    public readonly struct CombatSkillSlotState
    {
        internal CombatSkillSlotState(
            SkillSlot slot,
            SkillDefinition skill,
            SkillLevelDefinition level,
            float cooldownRemaining,
            bool isActorBusy,
            bool isActive,
            SkillUsePhase? activePhase,
            bool isReady)
        {
            Slot = slot;
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Level = level ?? throw new ArgumentNullException(nameof(level));
            CooldownRemaining = cooldownRemaining;
            IsActorBusy = isActorBusy;
            IsActive = isActive;
            ActivePhase = activePhase;
            IsReady = isReady;
        }

        public SkillSlot Slot { get; }
        public SkillDefinition Skill { get; }
        public SkillLevelDefinition Level { get; }
        public float CooldownDuration => Level.Cooldown;
        public float CooldownRemaining { get; }
        public bool IsActorBusy { get; }
        public bool IsActive { get; }
        public SkillUsePhase? ActivePhase { get; }
        public bool IsReady { get; }
    }

    public sealed class ActorCombatController : IDisposable
    {
        private readonly ActorInstance _actor;
        private readonly SkillExecutionController _execution;
        private readonly Dictionary<SkillSlot, RuntimeSkillSlot> _slots;
        private readonly RuntimeSkillSlot[] _orderedSlots;
        private readonly CombatSkillSlotState[] _slotStates;
        private readonly ReadOnlyCollection<CombatSkillSlotState> _readOnlySlotStates;
        private SkillUseHandle _activeUse;
        private RuntimeSkillSlot _activeRuntimeSlot;
        private bool _activeCooldownConsumed;
        private bool _isDisposed;

        public ActorCombatController(
            ActorInstance actor,
            SkillCatalog catalog,
            string loadoutId,
            SkillExecutionController execution)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));

            var loadout = catalog.RequireLoadout(loadoutId);
            _slots = new Dictionary<SkillSlot, RuntimeSkillSlot>(loadout.Slots.Count);
            _orderedSlots = new RuntimeSkillSlot[loadout.Slots.Count];
            _slotStates = new CombatSkillSlotState[loadout.Slots.Count];
            _readOnlySlotStates = Array.AsReadOnly(_slotStates);
            for (var index = 0; index < loadout.Slots.Count; index++)
            {
                var resolved = catalog.Resolve(loadoutId, loadout.Slots[index].Slot);
                var runtimeSlot = new RuntimeSkillSlot(index, resolved);
                _slots.Add(resolved.Slot, runtimeSlot);
                _orderedSlots[index] = runtimeSlot;
            }

            RefreshSlotStates();
        }

        public ActorInstance Actor => _actor;

        public IReadOnlyList<CombatSkillSlotState> Slots
        {
            get
            {
                RefreshObservableState();
                return _readOnlySlotStates;
            }
        }

        public bool IsBusy
        {
            get
            {
                RefreshObservableState();
                return _activeUse != null;
            }
        }

        public SkillSlot? ActiveSlot
        {
            get
            {
                RefreshObservableState();
                return _activeRuntimeSlot?.Slot;
            }
        }

        public SkillUsePhase? ActivePhase
        {
            get
            {
                RefreshObservableState();
                return _activeUse?.Phase;
            }
        }

        public bool CanCancelActiveUse
        {
            get
            {
                RefreshObservableState();
                return _activeUse != null && !_activeUse.HasCommitted;
            }
        }

        public bool HasSlot(SkillSlot slot)
        {
            RequireNotDisposed();
            return _slots.ContainsKey(slot);
        }

        public float GetRange(SkillSlot slot) => RequireSlot(slot).Level.Range;

        public CombatSkillSlotState GetSlotState(SkillSlot slot)
        {
            RefreshObservableState();
            return _slotStates[RequireSlot(slot).Index];
        }

        public bool IsReady(SkillSlot slot)
        {
            RequireNotDisposed();
            RefreshObservableState();
            return _slotStates[RequireSlot(slot).Index].IsReady;
        }

        public bool CanTarget(
            SkillSlot slot,
            ActorInstance target,
            SkillTargetRelation targetRelation)
        {
            RequireNotDisposed();
            var runtimeSlot = RequireSlot(slot);
            return IsValidTarget(
                runtimeSlot.Skill,
                new SkillUseRequest(slot, target, targetRelation, hasClearLine: true));
        }

        public void Tick(float deltaTime)
        {
            RequireNotDisposed();
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            for (var index = 0; index < _orderedSlots.Length; index++)
            {
                _orderedSlots[index].Cooldown.Tick(deltaTime);
            }

            RefreshObservableState();
        }

        public SkillUseResult TryUse(SkillUseRequest request)
        {
            RequireNotDisposed();
            RefreshActiveUse();
            var runtimeSlot = RequireSlot(request.Slot);
            if (!IsValidTarget(runtimeSlot.Skill, request))
            {
                RefreshSlotStates();
                return SkillUseResult.InvalidTarget;
            }

            if (_activeUse != null)
            {
                RefreshSlotStates();
                return SkillUseResult.Busy;
            }

            if (!runtimeSlot.Cooldown.IsReady)
            {
                RefreshSlotStates();
                return SkillUseResult.OnCooldown;
            }

            if (PlanarSqrDistance(_actor.Position, request.Target.Position) >
                runtimeSlot.Level.Range * runtimeSlot.Level.Range)
            {
                RefreshSlotStates();
                return SkillUseResult.OutOfRange;
            }

            if (request.TargetRelation != SkillTargetRelation.Self &&
                !request.HasClearLine)
            {
                RefreshSlotStates();
                return SkillUseResult.Blocked;
            }

            _actor.TryFaceTowards(request.Target.Position);
            _activeUse = _execution.Begin(
                _actor,
                request.Target,
                runtimeSlot.Skill,
                runtimeSlot.Level);
            _activeRuntimeSlot = runtimeSlot;
            _activeCooldownConsumed = false;
            RefreshObservableState();
            return SkillUseResult.Executed;
        }

        public bool CancelActiveUse()
        {
            RequireNotDisposed();
            RefreshActiveUse();
            if (_activeUse == null || _activeUse.HasCommitted)
                return false;

            var cancelled = _activeUse.TryCancel();
            RefreshObservableState();
            return cancelled;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _activeUse?.TryCancel();
            _activeUse = null;
            _activeRuntimeSlot = null;
            _isDisposed = true;
        }

        private RuntimeSkillSlot RequireSlot(SkillSlot slot)
        {
            if (!_slots.TryGetValue(slot, out var runtimeSlot))
            {
                throw new InvalidOperationException(
                    $"Actor combat loadout does not contain slot '{slot}'.");
            }

            return runtimeSlot;
        }

        private void RefreshActiveUse()
        {
            if (_activeUse == null)
                return;

            if (_activeUse.HasCommitted && !_activeCooldownConsumed)
            {
                if (!_activeRuntimeSlot.Cooldown.TryConsume())
                {
                    throw new InvalidOperationException(
                        "Committed skill use could not consume its reserved cooldown.");
                }

                _activeCooldownConsumed = true;
            }

            if (_activeUse.IsActive)
                return;

            _activeUse = null;
            _activeRuntimeSlot = null;
            _activeCooldownConsumed = false;
        }

        private void RefreshObservableState()
        {
            RequireNotDisposed();
            RefreshActiveUse();
            RefreshSlotStates();
        }

        private void RefreshSlotStates()
        {
            var isBusy = _activeUse != null;
            var activePhase = _activeUse?.Phase;
            for (var index = 0; index < _orderedSlots.Length; index++)
            {
                var runtimeSlot = _orderedSlots[index];
                var isActive = ReferenceEquals(runtimeSlot, _activeRuntimeSlot);
                _slotStates[index] = new CombatSkillSlotState(
                    runtimeSlot.Slot,
                    runtimeSlot.Skill,
                    runtimeSlot.Level,
                    runtimeSlot.Cooldown.Remaining,
                    isBusy,
                    isActive,
                    isActive ? activePhase : null,
                    _actor.IsAlive && !isBusy && runtimeSlot.Cooldown.IsReady);
            }
        }

        private bool IsValidTarget(
            SkillDefinition skill,
            SkillUseRequest request)
        {
            if (!_actor.IsAlive || request.Target == null || !request.Target.IsAlive)
                return false;

            var isSelf = ReferenceEquals(_actor, request.Target);
            if (skill is DirectHealSkillDefinition &&
                request.Target.CurrentHealth >= request.Target.MaximumHealth)
            {
                return false;
            }

            return skill.TargetRule switch
            {
                SkillTargetRule.EnemyActor =>
                    request.TargetRelation == SkillTargetRelation.Enemy && !isSelf,
                SkillTargetRule.AllyOrSelfActor => request.TargetRelation switch
                {
                    SkillTargetRelation.Self => isSelf,
                    SkillTargetRelation.Ally => !isSelf,
                    _ => false
                },
                _ => throw new InvalidOperationException(
                    $"Skill target rule '{skill.TargetRule}' is unsupported.")
            };
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ActorCombatController));
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }

        private sealed class RuntimeSkillSlot
        {
            public RuntimeSkillSlot(int index, ResolvedSkillSlot resolved)
            {
                Index = index;
                Slot = resolved.Slot;
                Skill = resolved.Skill;
                Level = resolved.Level;
                Cooldown = new SkillCooldown(Level.Cooldown);
            }

            public int Index { get; }
            public SkillSlot Slot { get; }
            public SkillDefinition Skill { get; }
            public SkillLevelDefinition Level { get; }
            public SkillCooldown Cooldown { get; }
        }
    }
}

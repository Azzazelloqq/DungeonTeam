using System;
using System.Collections.Generic;
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

    public sealed class ActorCombatController : IDisposable
    {
        private readonly ActorInstance _actor;
        private readonly SkillExecutionController _execution;
        private readonly Dictionary<SkillSlot, RuntimeSkillSlot> _slots;
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
            for (var index = 0; index < loadout.Slots.Count; index++)
            {
                var resolved = catalog.Resolve(loadoutId, loadout.Slots[index].Slot);
                _slots.Add(resolved.Slot, new RuntimeSkillSlot(resolved));
            }
        }

        public ActorInstance Actor => _actor;

        public bool HasSlot(SkillSlot slot) => _slots.ContainsKey(slot);

        public float GetRange(SkillSlot slot) => RequireSlot(slot).Level.Range;

        public int GetDamage(SkillSlot slot) => RequireSlot(slot).Level.Damage;

        public bool IsReady(SkillSlot slot)
        {
            RequireNotDisposed();
            RefreshActiveUse();
            return _activeUse == null && RequireSlot(slot).Cooldown.IsReady;
        }

        public void Tick(float deltaTime)
        {
            RequireNotDisposed();
            foreach (var slot in _slots.Values)
            {
                slot.Cooldown.Tick(deltaTime);
            }

            RefreshActiveUse();
        }

        public SkillUseResult TryUse(
            SkillSlot slot,
            ActorInstance target,
            bool hasClearLine)
        {
            RequireNotDisposed();
            RefreshActiveUse();
            var runtimeSlot = RequireSlot(slot);
            if (!_actor.IsAlive ||
                target == null ||
                !target.IsAlive ||
                ReferenceEquals(_actor, target) ||
                runtimeSlot.Skill.TargetRule != SkillTargetRule.EnemyActor)
            {
                return SkillUseResult.InvalidTarget;
            }

            if (PlanarSqrDistance(_actor.Position, target.Position) >
                runtimeSlot.Level.Range * runtimeSlot.Level.Range)
            {
                return SkillUseResult.OutOfRange;
            }

            if (!hasClearLine)
            {
                return SkillUseResult.Blocked;
            }

            if (_activeUse != null)
            {
                return SkillUseResult.Busy;
            }

            if (!runtimeSlot.Cooldown.IsReady)
            {
                return SkillUseResult.OnCooldown;
            }

            _actor.TryFaceTowards(target.Position);
            _activeUse = _execution.Begin(
                _actor,
                target,
                runtimeSlot.Skill,
                runtimeSlot.Level);
            _activeRuntimeSlot = runtimeSlot;
            _activeCooldownConsumed = false;
            RefreshActiveUse();
            return SkillUseResult.Executed;
        }

        public bool CancelActiveUse()
        {
            RequireNotDisposed();
            RefreshActiveUse();
            if (_activeUse == null || _activeUse.HasCommitted)
                return false;

            var cancelled = _activeUse.TryCancel();
            RefreshActiveUse();
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
            public RuntimeSkillSlot(ResolvedSkillSlot resolved)
            {
                Skill = resolved.Skill;
                Level = resolved.Level;
                Cooldown = new SkillCooldown(Level.Cooldown);
            }

            public SkillDefinition Skill { get; }
            public SkillLevelDefinition Level { get; }
            public SkillCooldown Cooldown { get; }
        }
    }
}

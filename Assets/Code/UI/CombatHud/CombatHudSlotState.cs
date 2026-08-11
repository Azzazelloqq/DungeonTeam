using System;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud
{
    public readonly struct CombatHudSlotState : IEquatable<CombatHudSlotState>
    {
        public CombatHudSlotState(
            SkillSlot slot,
            string title,
            Texture2D icon,
            float cooldownDuration,
            float cooldownRemaining,
            bool isReady,
            bool isSelected,
            bool isPending,
            SkillUsePhase? activePhase,
            bool isActorBusy)
        {
            if (!IsDefined(slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Combat HUD slot title cannot be empty.", nameof(title));
            if (!IsPositiveFinite(cooldownDuration))
                throw new ArgumentOutOfRangeException(nameof(cooldownDuration));
            if (!IsNonNegativeFinite(cooldownRemaining))
                throw new ArgumentOutOfRangeException(nameof(cooldownRemaining));
            if (activePhase.HasValue &&
                activePhase.Value is not SkillUsePhase.Preparing and
                    not SkillUsePhase.Recovering)
            {
                throw new ArgumentOutOfRangeException(nameof(activePhase));
            }

            Slot = slot;
            Title = title;
            Icon = icon;
            CooldownDuration = cooldownDuration;
            CooldownRemaining = Math.Min(cooldownRemaining, cooldownDuration);
            IsReady = isReady;
            IsSelected = isSelected;
            IsPending = isPending;
            ActivePhase = activePhase;
            IsActorBusy = isActorBusy;
        }

        public SkillSlot Slot { get; }
        public string Title { get; }
        public Texture2D Icon { get; }
        public float CooldownDuration { get; }
        public float CooldownRemaining { get; }
        public float CooldownProgress => CooldownRemaining / CooldownDuration;
        public bool IsReady { get; }
        public bool IsSelected { get; }
        public bool IsPending { get; }
        public SkillUsePhase? ActivePhase { get; }
        public bool IsActive => ActivePhase.HasValue;
        public bool IsActorBusy { get; }

        public bool Equals(CombatHudSlotState other)
        {
            return Slot == other.Slot &&
                   string.Equals(Title, other.Title, StringComparison.Ordinal) &&
                   ReferenceEquals(Icon, other.Icon) &&
                   CooldownDuration.Equals(other.CooldownDuration) &&
                   CooldownRemaining.Equals(other.CooldownRemaining) &&
                   IsReady == other.IsReady &&
                   IsSelected == other.IsSelected &&
                   IsPending == other.IsPending &&
                   ActivePhase == other.ActivePhase &&
                   IsActorBusy == other.IsActorBusy;
        }

        public override bool Equals(object obj)
        {
            return obj is CombatHudSlotState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Slot,
                Title,
                Icon,
                CooldownDuration,
                CooldownRemaining,
                IsReady,
                IsSelected,
                HashCode.Combine(IsPending, ActivePhase, IsActorBusy));
        }

        private static bool IsPositiveFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static bool IsDefined(SkillSlot slot)
        {
            return slot is SkillSlot.Primary or SkillSlot.Active1;
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}

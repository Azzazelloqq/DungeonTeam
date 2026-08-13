using System;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud
{
    public enum CombatHudTargetSelection
    {
        Automatic = 0,
        Manual = 1
    }

    public readonly struct CombatHudTargetState : IEquatable<CombatHudTargetState>
    {
        public CombatHudTargetState(
            Vector3 screenPosition,
            CombatHudTargetSelection selection)
        {
            if (!IsFinite(screenPosition.x) ||
                !IsFinite(screenPosition.y) ||
                !IsFinite(screenPosition.z))
                throw new ArgumentOutOfRangeException(nameof(screenPosition));
            if (selection is not CombatHudTargetSelection.Automatic and
                not CombatHudTargetSelection.Manual)
                throw new ArgumentOutOfRangeException(nameof(selection));

            ScreenPosition = new Vector2(screenPosition.x, screenPosition.y);
            Selection = selection;
            HasTarget = true;
            IsInFront = screenPosition.z > 0f;
        }

        public static CombatHudTargetState Hidden => default;

        public Vector2 ScreenPosition { get; }

        public CombatHudTargetSelection Selection { get; }

        public bool HasTarget { get; }

        public bool IsInFront { get; }

        public bool Equals(CombatHudTargetState other)
        {
            return ScreenPosition.Equals(other.ScreenPosition) &&
                   Selection == other.Selection &&
                   HasTarget == other.HasTarget &&
                   IsInFront == other.IsInFront;
        }

        public override bool Equals(object obj)
        {
            return obj is CombatHudTargetState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                ScreenPosition,
                (int)Selection,
                HasTarget,
                IsInFront);
        }

        public static bool operator ==(
            CombatHudTargetState left,
            CombatHudTargetState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CombatHudTargetState left,
            CombatHudTargetState right)
        {
            return !left.Equals(right);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

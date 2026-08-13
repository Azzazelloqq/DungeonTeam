using System;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud
{
    internal readonly struct CombatHudTargetMarkerLayoutResult
    {
        public CombatHudTargetMarkerLayoutResult(
            Vector2 position,
            Vector2 direction,
            bool isOffscreen)
        {
            Position = position;
            Direction = direction;
            IsOffscreen = isOffscreen;
            IsVisible = true;
        }

        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public bool IsOffscreen { get; }
        public bool IsVisible { get; }
    }

    internal static class CombatHudTargetMarkerLayout
    {
        private const float DirectionThreshold = 0.0001f;

        public static CombatHudTargetMarkerLayoutResult Resolve(
            Rect safeArea,
            Vector2 projectedPosition,
            bool isInFront,
            float markerHalfSize)
        {
            if (float.IsNaN(markerHalfSize) ||
                float.IsInfinity(markerHalfSize) ||
                markerHalfSize < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(markerHalfSize));
            }

            var availableHalfWidth = safeArea.width * 0.5f - markerHalfSize;
            var availableHalfHeight = safeArea.height * 0.5f - markerHalfSize;
            if (availableHalfWidth <= 0f || availableHalfHeight <= 0f)
                return default;

            var center = safeArea.center;
            var offset = projectedPosition - center;
            var isInside = Mathf.Abs(offset.x) <= availableHalfWidth &&
                           Mathf.Abs(offset.y) <= availableHalfHeight;
            if (isInFront && isInside)
            {
                return new CombatHudTargetMarkerLayoutResult(
                    projectedPosition,
                    Vector2.zero,
                    isOffscreen: false);
            }

            var direction = isInFront ? offset : -offset;
            if (direction.sqrMagnitude <= DirectionThreshold)
                direction = Vector2.up;

            direction.Normalize();
            var horizontalScale = Mathf.Abs(direction.x) > DirectionThreshold
                ? availableHalfWidth / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            var verticalScale = Mathf.Abs(direction.y) > DirectionThreshold
                ? availableHalfHeight / Mathf.Abs(direction.y)
                : float.PositiveInfinity;
            var edgeScale = Mathf.Min(horizontalScale, verticalScale);
            return new CombatHudTargetMarkerLayoutResult(
                center + direction * edgeScale,
                direction,
                isOffscreen: true);
        }
    }
}

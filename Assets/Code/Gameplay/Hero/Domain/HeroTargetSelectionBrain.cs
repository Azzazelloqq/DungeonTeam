using System;

namespace DungeonTeam.Gameplay.Hero.Domain
{
    public enum HeroTargetSelectionMode
    {
        Automatic,
        Manual
    }

    public sealed class HeroTargetSelectionBrain
    {
        private readonly float _manualTargetLossDistance;
        private readonly float _unreachableGraceDuration;
        private float _unreachableDuration;

        public HeroTargetSelectionBrain(
            float manualTargetLossDistance,
            float unreachableGraceDuration)
        {
            if (float.IsNaN(manualTargetLossDistance) ||
                float.IsInfinity(manualTargetLossDistance) ||
                manualTargetLossDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(manualTargetLossDistance));
            }

            if (float.IsNaN(unreachableGraceDuration) ||
                float.IsInfinity(unreachableGraceDuration) ||
                unreachableGraceDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unreachableGraceDuration));
            }

            _manualTargetLossDistance = manualTargetLossDistance;
            _unreachableGraceDuration = unreachableGraceDuration;
        }

        public HeroTargetSelectionMode Mode { get; private set; }

        public void SelectManual()
        {
            Mode = HeroTargetSelectionMode.Manual;
            _unreachableDuration = 0f;
        }

        public void UseAutomatic()
        {
            Mode = HeroTargetSelectionMode.Automatic;
            _unreachableDuration = 0f;
        }

        public HeroTargetSelectionMode Evaluate(
            bool hasTarget,
            bool isTargetAlive,
            bool isReachable,
            float targetDistance,
            float deltaTime)
        {
            if (float.IsNaN(targetDistance) ||
                float.IsInfinity(targetDistance) ||
                targetDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDistance));
            }

            if (float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime) ||
                deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (Mode != HeroTargetSelectionMode.Manual)
            {
                return Mode;
            }

            if (!hasTarget ||
                !isTargetAlive ||
                targetDistance > _manualTargetLossDistance)
            {
                UseAutomatic();
                return Mode;
            }

            if (isReachable)
            {
                _unreachableDuration = 0f;
                return Mode;
            }

            _unreachableDuration += deltaTime;
            if (_unreachableDuration >= _unreachableGraceDuration)
                UseAutomatic();

            return Mode;
        }
    }
}

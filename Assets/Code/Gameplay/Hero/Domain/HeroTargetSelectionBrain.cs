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

        public HeroTargetSelectionBrain(float manualTargetLossDistance)
        {
            if (float.IsNaN(manualTargetLossDistance) ||
                float.IsInfinity(manualTargetLossDistance) ||
                manualTargetLossDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(manualTargetLossDistance));
            }

            _manualTargetLossDistance = manualTargetLossDistance;
        }

        public HeroTargetSelectionMode Mode { get; private set; }

        public void SelectManual()
        {
            Mode = HeroTargetSelectionMode.Manual;
        }

        public void UseAutomatic()
        {
            Mode = HeroTargetSelectionMode.Automatic;
        }

        public HeroTargetSelectionMode Evaluate(bool hasValidTarget, float targetDistance)
        {
            if (float.IsNaN(targetDistance) ||
                float.IsInfinity(targetDistance) ||
                targetDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDistance));
            }

            if (Mode == HeroTargetSelectionMode.Manual &&
                (!hasValidTarget || targetDistance > _manualTargetLossDistance))
            {
                Mode = HeroTargetSelectionMode.Automatic;
            }

            return Mode;
        }
    }
}

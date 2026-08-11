using System;

namespace DungeonTeam.Gameplay.Hero.Domain
{
    public enum HeroSkillActionState
    {
        Idle,
        Approaching,
        ReadyToUse
    }

    public sealed class HeroSkillActionBrain
    {
        private float _range;

        public HeroSkillActionState State { get; private set; }

        public void Request(float range)
        {
            if (float.IsNaN(range) || float.IsInfinity(range) || range <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(range));
            }

            _range = range;
            State = HeroSkillActionState.Approaching;
        }

        public HeroSkillActionState Evaluate(
            bool targetAlive,
            float distanceToTarget,
            bool hasClearLine)
        {
            if (distanceToTarget < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
            }

            if (State == HeroSkillActionState.Idle)
            {
                return State;
            }

            if (!targetAlive)
            {
                Cancel();
                return State;
            }

            State = distanceToTarget <= _range && hasClearLine
                ? HeroSkillActionState.ReadyToUse
                : HeroSkillActionState.Approaching;

            return State;
        }

        public bool ConsumeUse()
        {
            if (State != HeroSkillActionState.ReadyToUse)
            {
                return false;
            }

            State = HeroSkillActionState.Idle;
            return true;
        }

        public void Cancel()
        {
            State = HeroSkillActionState.Idle;
        }
    }
}

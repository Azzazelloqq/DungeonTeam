using System;

namespace DungeonTeam.Gameplay.Hero.Domain
{
    public enum HeroBasicAttackState
    {
        Idle,
        Approaching,
        ReadyToAttack
    }

    public sealed class HeroBasicAttackBrain
    {
        private readonly float _attackRange;

        public HeroBasicAttackBrain(float attackRange)
        {
            if (attackRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            }

            _attackRange = attackRange;
        }

        public HeroBasicAttackState State { get; private set; }

        public void RequestAttack()
        {
            State = HeroBasicAttackState.Approaching;
        }

        public HeroBasicAttackState Evaluate(
            bool targetAlive,
            float distanceToTarget,
            bool hasClearLine)
        {
            if (distanceToTarget < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
            }

            if (State == HeroBasicAttackState.Idle)
            {
                return State;
            }

            if (!targetAlive)
            {
                Cancel();
                return State;
            }

            State = distanceToTarget <= _attackRange && hasClearLine
                ? HeroBasicAttackState.ReadyToAttack
                : HeroBasicAttackState.Approaching;

            return State;
        }

        public bool ConsumeAttack()
        {
            if (State != HeroBasicAttackState.ReadyToAttack)
            {
                return false;
            }

            State = HeroBasicAttackState.Idle;
            return true;
        }

        public void Cancel()
        {
            State = HeroBasicAttackState.Idle;
        }
    }
}

using System;

namespace DungeonTeam.Gameplay.Team.Domain
{
    public enum CompanionCombatState
    {
        Follow,
        Chase,
        Attack
    }

    public sealed class CompanionCombatBrain
    {
        private readonly float _attackRange;
        private readonly float _targetLossDistance;

        public CompanionCombatBrain(float attackRange, float targetLossDistance)
        {
            if (attackRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            }

            if (targetLossDistance <= attackRange)
            {
                throw new ArgumentOutOfRangeException(nameof(targetLossDistance));
            }

            _attackRange = attackRange;
            _targetLossDistance = targetLossDistance;
        }

        public CompanionCombatState Evaluate(
            bool hasTarget,
            bool hasClearLine,
            float distanceToTarget)
        {
            if (distanceToTarget < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
            }

            if (!hasTarget || distanceToTarget > _targetLossDistance)
            {
                return CompanionCombatState.Follow;
            }

            return distanceToTarget <= _attackRange && hasClearLine
                ? CompanionCombatState.Attack
                : CompanionCombatState.Chase;
        }
    }
}

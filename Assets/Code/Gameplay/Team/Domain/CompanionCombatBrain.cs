using System;

namespace DungeonTeam.Gameplay.Team.Domain
{
    public enum CompanionCombatState
    {
        Follow,
        Chase,
        UseSkill
    }

    public sealed class CompanionCombatBrain
    {
        private readonly float _targetLossDistance;

        public CompanionCombatBrain(float targetLossDistance)
        {
            if (float.IsNaN(targetLossDistance) ||
                float.IsInfinity(targetLossDistance) ||
                targetLossDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetLossDistance));
            }

            _targetLossDistance = targetLossDistance;
        }

        public CompanionCombatState Evaluate(
            bool hasTarget,
            bool hasClearLine,
            float distanceToTarget,
            float skillRange)
        {
            if (float.IsNaN(distanceToTarget) ||
                float.IsInfinity(distanceToTarget) ||
                distanceToTarget < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
            }

            if (float.IsNaN(skillRange) ||
                float.IsInfinity(skillRange) ||
                skillRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(skillRange));
            }

            if (!hasTarget || distanceToTarget > _targetLossDistance)
            {
                return CompanionCombatState.Follow;
            }

            return distanceToTarget <= skillRange && hasClearLine
                ? CompanionCombatState.UseSkill
                : CompanionCombatState.Chase;
        }
    }
}

using System;

namespace DungeonTeam.Gameplay.EnemyAI.Domain
{
    public enum EnemyAiState
    {
        Idle,
        Chase,
        Attack,
        Return
    }

    public sealed class EnemyAiBrain
    {
        private readonly float _attackRange;
        private readonly float _targetLossDistance;
        private readonly float _homeArrivalDistance;

        public EnemyAiBrain(
            float attackRange,
            float targetLossDistance,
            float homeArrivalDistance)
        {
            if (attackRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            }

            if (targetLossDistance <= attackRange)
            {
                throw new ArgumentOutOfRangeException(nameof(targetLossDistance));
            }

            if (homeArrivalDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(homeArrivalDistance));
            }

            _attackRange = attackRange;
            _targetLossDistance = targetLossDistance;
            _homeArrivalDistance = homeArrivalDistance;
        }

        public EnemyAiState State { get; private set; }

        public EnemyAiState Evaluate(
            bool hasTarget,
            bool targetDetected,
            bool canAttackTarget,
            float distanceToTarget,
            float distanceToHome)
        {
            if (distanceToTarget < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToTarget));
            }

            if (distanceToHome < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToHome));
            }

            State = State switch
            {
                EnemyAiState.Idle => EvaluateIdle(
                    hasTarget,
                    targetDetected,
                    canAttackTarget,
                    distanceToTarget),
                EnemyAiState.Chase or EnemyAiState.Attack => EvaluateEngaged(
                    hasTarget,
                    canAttackTarget,
                    distanceToTarget),
                EnemyAiState.Return => EvaluateReturn(
                    hasTarget,
                    targetDetected,
                    canAttackTarget,
                    distanceToTarget,
                    distanceToHome),
                _ => throw new ArgumentOutOfRangeException()
            };

            return State;
        }

        private EnemyAiState EvaluateIdle(
            bool hasTarget,
            bool targetDetected,
            bool canAttackTarget,
            float distanceToTarget)
        {
            return hasTarget && targetDetected
                ? EvaluateCombatDistance(distanceToTarget, canAttackTarget)
                : EnemyAiState.Idle;
        }

        private EnemyAiState EvaluateEngaged(
            bool hasTarget,
            bool canAttackTarget,
            float distanceToTarget)
        {
            if (!hasTarget || distanceToTarget > _targetLossDistance)
            {
                return EnemyAiState.Return;
            }

            return EvaluateCombatDistance(distanceToTarget, canAttackTarget);
        }

        private EnemyAiState EvaluateReturn(
            bool hasTarget,
            bool targetDetected,
            bool canAttackTarget,
            float distanceToTarget,
            float distanceToHome)
        {
            if (hasTarget && targetDetected)
            {
                return EvaluateCombatDistance(distanceToTarget, canAttackTarget);
            }

            return distanceToHome <= _homeArrivalDistance
                ? EnemyAiState.Idle
                : EnemyAiState.Return;
        }

        private EnemyAiState EvaluateCombatDistance(
            float distanceToTarget,
            bool canAttackTarget)
        {
            return distanceToTarget <= _attackRange && canAttackTarget
                ? EnemyAiState.Attack
                : EnemyAiState.Chase;
        }
    }
}

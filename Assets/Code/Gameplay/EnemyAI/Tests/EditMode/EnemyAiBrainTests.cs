using DungeonTeam.Gameplay.EnemyAI.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.EnemyAI.Tests
{
    public sealed class EnemyAiBrainTests
    {
        [Test]
        public void Evaluate_WhenTargetIsNotDetected_RemainsIdle()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(
                hasTarget: false,
                targetDetected: false,
                canAttackTarget: false,
                distanceToTarget: 0f,
                distanceToHome: 0f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Idle));
        }

        [Test]
        public void Evaluate_WhenDetectedTargetIsOutsideAttackRange_StartsChasing()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: true,
                canAttackTarget: true,
                distanceToTarget: 5f,
                distanceToHome: 0f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Chase));
        }

        [Test]
        public void Evaluate_WhenDetectedTargetIsInsideAttackRange_StartsAttacking()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: true,
                canAttackTarget: true,
                distanceToTarget: 1f,
                distanceToHome: 0f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Attack));
        }

        [Test]
        public void Evaluate_WhileChasingAndTargetIsHiddenButClose_ContinuesChasing()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 5f, distanceToHome: 0f);

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: false,
                canAttackTarget: false,
                distanceToTarget: 6f,
                distanceToHome: 2f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Chase));
        }

        [Test]
        public void Evaluate_WhileChasingAndTargetExceedsLossDistance_StartsReturning()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 5f, distanceToHome: 0f);

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: false,
                canAttackTarget: false,
                distanceToTarget: 11f,
                distanceToHome: 4f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Return));
        }

        [Test]
        public void Evaluate_WhileAttackingAndTargetLeavesAttackRange_StartsChasing()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 1f, distanceToHome: 0f);

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: false,
                canAttackTarget: true,
                distanceToTarget: 2f,
                distanceToHome: 2f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Chase));
        }

        [Test]
        public void Evaluate_WhenCloseTargetIsBehindObstacle_ContinuesChasing()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 5f, distanceToHome: 0f);

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: false,
                canAttackTarget: false,
                distanceToTarget: 1f,
                distanceToHome: 2f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Chase));
        }

        [Test]
        public void Evaluate_WhileReturningAndHomeIsReached_BecomesIdle()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 5f, distanceToHome: 0f);
            brain.Evaluate(true, false, false, distanceToTarget: 11f, distanceToHome: 4f);

            var state = brain.Evaluate(
                hasTarget: false,
                targetDetected: false,
                canAttackTarget: false,
                distanceToTarget: 0f,
                distanceToHome: 0.25f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Idle));
        }

        [Test]
        public void Evaluate_WhileReturningAndNewTargetIsDetected_StartsChasing()
        {
            var brain = CreateBrain();
            brain.Evaluate(true, true, true, distanceToTarget: 5f, distanceToHome: 0f);
            brain.Evaluate(true, false, false, distanceToTarget: 11f, distanceToHome: 4f);

            var state = brain.Evaluate(
                hasTarget: true,
                targetDetected: true,
                canAttackTarget: true,
                distanceToTarget: 4f,
                distanceToHome: 3f);

            Assert.That(state, Is.EqualTo(EnemyAiState.Chase));
        }

        private static EnemyAiBrain CreateBrain()
        {
            return new EnemyAiBrain(
                attackRange: 1.5f,
                targetLossDistance: 10f,
                homeArrivalDistance: 0.25f);
        }
    }

}

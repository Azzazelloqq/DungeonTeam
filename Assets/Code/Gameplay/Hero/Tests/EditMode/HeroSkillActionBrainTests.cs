using DungeonTeam.Gameplay.Hero.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Hero.Tests
{
    public sealed class HeroSkillActionBrainTests
    {
        [Test]
        public void Evaluate_AfterAttackRequestedOutsideRange_StartsApproaching()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);

            var state = brain.Evaluate(
                targetAlive: true,
                distanceToTarget: 5f,
                hasClearLine: true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.Approaching));
        }

        [Test]
        public void Evaluate_AfterAttackRequestedInsideRangeAndClearLine_BecomesReadyToAttack()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);

            var state = brain.Evaluate(
                targetAlive: true,
                distanceToTarget: 1f,
                hasClearLine: true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.ReadyToUse));
        }

        [Test]
        public void Evaluate_WhenCloseTargetIsBehindObstacle_ContinuesApproaching()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);

            var state = brain.Evaluate(
                targetAlive: true,
                distanceToTarget: 1f,
                hasClearLine: false);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.Approaching));
        }

        [Test]
        public void Evaluate_WhenTargetDies_CancelsAttack()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);
            brain.Evaluate(true, 5f, true);

            var state = brain.Evaluate(
                targetAlive: false,
                distanceToTarget: 5f,
                hasClearLine: true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.Idle));
        }

        [Test]
        public void Cancel_WhileApproaching_ReturnsToIdleAndDoesNotResumeWithoutNewRequest()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);
            brain.Evaluate(true, 5f, true);

            brain.Cancel();
            var state = brain.Evaluate(true, 5f, true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.Idle));
        }

        [Test]
        public void ConsumeAttack_WhenReady_CompletesExactlyOnce()
        {
            var brain = CreateBrain();
            brain.Request(1.5f);
            brain.Evaluate(true, 1f, true);

            var firstResult = brain.ConsumeUse();
            var secondResult = brain.ConsumeUse();

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(brain.State, Is.EqualTo(HeroSkillActionState.Idle));
        }

        [Test]
        public void Evaluate_WithoutAttackRequest_RemainsIdle()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(true, 1f, true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.Idle));
        }

        [Test]
        public void Request_WhileApproaching_ReplacesRange()
        {
            var brain = CreateBrain();
            brain.Request(1f);
            brain.Evaluate(true, 2f, true);

            brain.Request(3f);
            var state = brain.Evaluate(true, 2f, true);

            Assert.That(state, Is.EqualTo(HeroSkillActionState.ReadyToUse));
        }

        private static HeroSkillActionBrain CreateBrain()
        {
            return new HeroSkillActionBrain();
        }
    }
}

using DungeonTeam.Gameplay.Team.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Team.Tests
{
    public sealed class CompanionCombatBrainTests
    {
        [Test]
        public void Evaluate_WithoutTarget_Follows()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(false, false, 0f);

            Assert.That(state, Is.EqualTo(CompanionCombatState.Follow));
        }

        [Test]
        public void Evaluate_WithDistantTarget_Chases()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(true, true, 5f);

            Assert.That(state, Is.EqualTo(CompanionCombatState.Chase));
        }

        [Test]
        public void Evaluate_WithCloseVisibleTarget_Attacks()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(true, true, 1f);

            Assert.That(state, Is.EqualTo(CompanionCombatState.Attack));
        }

        [Test]
        public void Evaluate_WithBlockedCloseTarget_Chases()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(true, false, 1f);

            Assert.That(state, Is.EqualTo(CompanionCombatState.Chase));
        }

        [Test]
        public void Evaluate_WhenTargetExceedsLossDistance_Follows()
        {
            var brain = CreateBrain();

            var state = brain.Evaluate(true, true, 13f);

            Assert.That(state, Is.EqualTo(CompanionCombatState.Follow));
        }

        private static CompanionCombatBrain CreateBrain()
        {
            return new CompanionCombatBrain(attackRange: 1.5f, targetLossDistance: 12f);
        }
    }
}

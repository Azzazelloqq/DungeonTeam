using DungeonTeam.Gameplay.Hero.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Hero.Tests
{
    public sealed class HeroTargetSelectionBrainTests
    {
        [Test]
        public void Evaluate_ManualTargetInsideLossDistance_RemainsManual()
        {
            var brain = new HeroTargetSelectionBrain(manualTargetLossDistance: 15f);
            brain.SelectManual();

            var mode = brain.Evaluate(hasValidTarget: true, targetDistance: 15f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Manual));
        }

        [Test]
        public void Evaluate_ManualTargetBeyondLossDistance_ReturnsAutomatic()
        {
            var brain = new HeroTargetSelectionBrain(manualTargetLossDistance: 15f);
            brain.SelectManual();

            var mode = brain.Evaluate(hasValidTarget: true, targetDistance: 15.01f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        [Test]
        public void Evaluate_InvalidManualTarget_ReturnsAutomatic()
        {
            var brain = new HeroTargetSelectionBrain(manualTargetLossDistance: 15f);
            brain.SelectManual();

            var mode = brain.Evaluate(hasValidTarget: false, targetDistance: 0f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        [Test]
        public void UseAutomatic_AfterManualSelection_ReturnsAutomatic()
        {
            var brain = new HeroTargetSelectionBrain(manualTargetLossDistance: 15f);
            brain.SelectManual();

            brain.UseAutomatic();

            Assert.That(brain.Mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }
    }
}

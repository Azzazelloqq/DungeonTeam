using DungeonTeam.Gameplay.Hero.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Hero.Tests
{
    public sealed class HeroTargetSelectionBrainTests
    {
        [Test]
        public void Evaluate_ManualTargetInsideLossDistance_RemainsManual()
        {
            var brain = CreateBrain();
            brain.SelectManual();

            var mode = brain.Evaluate(true, true, true, 15f, 0f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Manual));
        }

        [Test]
        public void Evaluate_ManualTargetBeyondLossDistance_ReturnsAutomatic()
        {
            var brain = CreateBrain();
            brain.SelectManual();

            var mode = brain.Evaluate(true, true, true, 15.01f, 0f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        [Test]
        public void Evaluate_InvalidManualTarget_ReturnsAutomatic()
        {
            var brain = CreateBrain();
            brain.SelectManual();

            var mode = brain.Evaluate(false, false, false, 0f, 0f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        [Test]
        public void UseAutomatic_AfterManualSelection_ReturnsAutomatic()
        {
            var brain = CreateBrain();
            brain.SelectManual();

            brain.UseAutomatic();

            Assert.That(brain.Mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        private static HeroTargetSelectionBrain CreateBrain()
        {
            return new HeroTargetSelectionBrain(
                manualTargetLossDistance: 15f,
                unreachableGraceDuration: 0.3f);
        }

        [Test]
        public void Evaluate_ManualTargetTemporarilyUnreachable_RemainsManual()
        {
            var brain = new HeroTargetSelectionBrain(
                manualTargetLossDistance: 15f,
                unreachableGraceDuration: 0.3f);
            brain.SelectManual();

            var mode = brain.Evaluate(
                hasTarget: true,
                isTargetAlive: true,
                isReachable: false,
                targetDistance: 5f,
                deltaTime: 0.1f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Manual));
        }

        [Test]
        public void Evaluate_ManualTargetUnreachableThroughGrace_ReturnsAutomatic()
        {
            var brain = new HeroTargetSelectionBrain(
                manualTargetLossDistance: 15f,
                unreachableGraceDuration: 0.3f);
            brain.SelectManual();

            brain.Evaluate(true, true, false, 5f, 0.2f);
            var mode = brain.Evaluate(true, true, false, 5f, 0.11f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }

        [Test]
        public void Evaluate_ReachabilityRestoredBeforeTimeout_ResetsGrace()
        {
            var brain = new HeroTargetSelectionBrain(
                manualTargetLossDistance: 15f,
                unreachableGraceDuration: 0.3f);
            brain.SelectManual();

            brain.Evaluate(true, true, false, 5f, 0.2f);
            brain.Evaluate(true, true, true, 5f, 0.1f);
            var mode = brain.Evaluate(true, true, false, 5f, 0.2f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Manual));
        }

        [Test]
        public void Evaluate_DeadManualTarget_ReturnsAutomaticImmediately()
        {
            var brain = new HeroTargetSelectionBrain(
                manualTargetLossDistance: 15f,
                unreachableGraceDuration: 0.3f);
            brain.SelectManual();

            var mode = brain.Evaluate(
                hasTarget: true,
                isTargetAlive: false,
                isReachable: true,
                targetDistance: 5f,
                deltaTime: 0f);

            Assert.That(mode, Is.EqualTo(HeroTargetSelectionMode.Automatic));
        }
    }
}

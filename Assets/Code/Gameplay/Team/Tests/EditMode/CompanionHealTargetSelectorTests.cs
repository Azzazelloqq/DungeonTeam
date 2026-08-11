using DungeonTeam.Gameplay.Team.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Team.Tests
{
    public sealed class CompanionHealTargetSelectorTests
    {
        [Test]
        public void Select_ReturnsLowestLivingHealthRatioBelowThreshold()
        {
            var candidates = new[]
            {
                new CompanionHealthSnapshot(50, 100),
                new CompanionHealthSnapshot(20, 50),
                new CompanionHealthSnapshot(0, 100),
                new CompanionHealthSnapshot(100, 100)
            };

            var selected = CompanionHealTargetSelector.Select(candidates, 0.6f);

            Assert.That(selected, Is.EqualTo(1));
        }

        [Test]
        public void Select_WithEqualRatio_PreservesStableTeamOrder()
        {
            var candidates = new[]
            {
                new CompanionHealthSnapshot(30, 60),
                new CompanionHealthSnapshot(50, 100)
            };

            var selected = CompanionHealTargetSelector.Select(candidates, 0.6f);

            Assert.That(selected, Is.Zero);
        }

        [Test]
        public void Select_WithNoCandidateAtThreshold_ReturnsMissingIndex()
        {
            var candidates = new[]
            {
                new CompanionHealthSnapshot(61, 100),
                new CompanionHealthSnapshot(80, 100)
            };

            Assert.That(
                CompanionHealTargetSelector.Select(candidates, 0.6f),
                Is.EqualTo(-1));
        }
    }
}

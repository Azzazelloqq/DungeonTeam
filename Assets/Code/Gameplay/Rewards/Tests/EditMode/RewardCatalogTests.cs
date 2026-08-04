using System;
using DungeonTeam.Gameplay.Rewards.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Rewards.Tests
{
    public sealed class RewardCatalogTests
    {
        [Test]
        public void Require_WithConfiguredReward_ReturnsDefinition()
        {
            var definition = new RewardDefinition("reward.gold", "Gold");
            var catalog = new RewardCatalog(new[] { definition });

            var resolved = catalog.Require("reward.gold");

            Assert.That(resolved, Is.SameAs(definition));
        }

        [Test]
        public void Create_WithDuplicateRewardId_Throws()
        {
            var definitions = new[]
            {
                new RewardDefinition("reward.gold", "Gold"),
                new RewardDefinition("reward.gold", "Other Gold")
            };

            Assert.Throws<ArgumentException>(() => new RewardCatalog(definitions));
        }

        [Test]
        public void Require_WithUnknownReward_Throws()
        {
            var catalog = new RewardCatalog(Array.Empty<RewardDefinition>());

            Assert.Throws<InvalidOperationException>(() => catalog.Require("reward.unknown"));
        }
    }
}

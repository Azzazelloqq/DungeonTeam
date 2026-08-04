using System;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Rewards.Tests
{
    public sealed class RewardPickupModelTests
    {
        [Test]
        public void Create_WithInvalidReward_Throws()
        {
            Assert.Throws<ArgumentException>(() => new RewardPickupModel(default));
        }

        [Test]
        public void TryCollect_CollectsPositiveAmountOnlyOnce()
        {
            var model = new RewardPickupModel(new RewardGrant("reward.gold", 3));
            model.Initialize();

            try
            {
                var firstResult = model.TryCollect(out var firstReward);
                var secondResult = model.TryCollect(out var secondReward);

                Assert.That(firstResult, Is.True);
                Assert.That(firstReward.RewardId, Is.EqualTo("reward.gold"));
                Assert.That(firstReward.Amount, Is.EqualTo(3));
                Assert.That(model.IsCollected, Is.True);
                Assert.That(secondResult, Is.False);
                Assert.That(secondReward, Is.EqualTo(default(RewardGrant)));
            }
            finally
            {
                model.Dispose();
            }
        }
    }
}

using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonContentPlanTests
    {
        [Test]
        public void Create_WhenSourceArrayChanges_KeepsOriginalPlan()
        {
            var pose = new DungeonPose(0f, 0f, 0f, 0f, 0f, 0f, 1f);
            var source = new[]
            {
                new EnemySpawnPlan("placement-1", "enemy-1", "encounter-1", pose)
            };

            var plan = new DungeonContentPlan(
                source,
                Array.Empty<InterestPointSpawnPlan>(),
                Array.Empty<ObjectiveSpawnPlan>(),
                rewardBudgetMultiplier: 1f);

            source[0] = new EnemySpawnPlan("placement-2", "enemy-2", "encounter-2", pose);

            Assert.That(plan.EnemySpawns[0].EnemyId, Is.EqualTo("enemy-1"));
        }

        [Test]
        public void Create_WithNegativeRewardMultiplier_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DungeonContentPlan(
                Array.Empty<EnemySpawnPlan>(),
                Array.Empty<InterestPointSpawnPlan>(),
                Array.Empty<ObjectiveSpawnPlan>(),
                rewardBudgetMultiplier: -1f));
        }
    }

    public sealed class DungeonMapSnapshotTests
    {
        [Test]
        public void Create_WithMissingDungeonId_Throws()
        {
            var pose = new DungeonPose(0f, 0f, 0f, 0f, 0f, 0f, 1f);

            Assert.Throws<ArgumentException>(() =>
                new DungeonMapSnapshot(" ", 42, pose, pose));
        }
    }
}

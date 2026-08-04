using System;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.DungeonRun.Tests
{
    public sealed class DungeonRunProgressTests
    {
        [Test]
        public void RecordEnemyKilled_WhenEnemiesRemain_UpdatesProgress()
        {
            var progress = new DungeonRunProgress(enemyCount: 2);

            var recorded = progress.RecordEnemyKilled();

            Assert.That(recorded, Is.True);
            Assert.That(progress.KilledEnemies, Is.EqualTo(1));
            Assert.That(progress.RemainingEnemies, Is.EqualTo(1));
        }

        [Test]
        public void RecordEnemyKilled_WhenAllEnemiesWereRecorded_DoesNotOvercount()
        {
            var progress = new DungeonRunProgress(enemyCount: 1);
            progress.RecordEnemyKilled();

            var recorded = progress.RecordEnemyKilled();

            Assert.That(recorded, Is.False);
            Assert.That(progress.KilledEnemies, Is.EqualTo(1));
            Assert.That(progress.RemainingEnemies, Is.Zero);
        }

        [Test]
        public void CollectReward_WithPositiveAmount_AddsToRunTotal()
        {
            var progress = new DungeonRunProgress(enemyCount: 0);

            progress.CollectReward(new RewardGrant("reward.gold", 2));
            progress.CollectReward(new RewardGrant("reward.gold", 3));

            Assert.That(progress.CollectedRewardCount, Is.EqualTo(5));
            Assert.That(progress.CreateCollectedRewardsSnapshot(), Has.Length.EqualTo(1));
            Assert.That(progress.CreateCollectedRewardsSnapshot()[0].RewardId,
                Is.EqualTo("reward.gold"));
            Assert.That(progress.CreateCollectedRewardsSnapshot()[0].Amount, Is.EqualTo(5));
        }

        [Test]
        public void CanExit_BecomesTrueOnlyAfterAllEnemiesWereKilled()
        {
            var progress = new DungeonRunProgress(enemyCount: 2);

            progress.RecordEnemyKilled();
            Assert.That(progress.CanExit, Is.False);

            progress.RecordEnemyKilled();
            Assert.That(progress.CanExit, Is.True);
        }

        [Test]
        public void TryFinish_CompletedBeforeObjectiveIsMet_IsRejected()
        {
            var progress = new DungeonRunProgress(enemyCount: 1);

            var finished = progress.TryFinish(DungeonRunOutcome.Completed);

            Assert.That(finished, Is.False);
            Assert.That(progress.IsFinished, Is.False);
        }

        [Test]
        public void TryFinish_AfterObjectiveIsMet_CompletesExactlyOnce()
        {
            var progress = new DungeonRunProgress(enemyCount: 1);
            progress.RecordEnemyKilled();

            var first = progress.TryFinish(DungeonRunOutcome.Completed);
            var second = progress.TryFinish(DungeonRunOutcome.Completed);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(progress.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
        }

        [Test]
        public void TryFinish_DefeatedWhileRunning_FinishesExactlyOnce()
        {
            var progress = new DungeonRunProgress(enemyCount: 2);

            var first = progress.TryFinish(DungeonRunOutcome.Defeated);
            var second = progress.TryFinish(DungeonRunOutcome.Defeated);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(progress.Outcome, Is.EqualTo(DungeonRunOutcome.Defeated));
        }

        [Test]
        public void Progress_AfterRunFinished_DoesNotChange()
        {
            var progress = new DungeonRunProgress(enemyCount: 1);
            progress.TryFinish(DungeonRunOutcome.Defeated);

            var killed = progress.RecordEnemyKilled();
            var collected = progress.CollectReward(new RewardGrant("reward.gold", 2));

            Assert.That(killed, Is.False);
            Assert.That(collected, Is.False);
            Assert.That(progress.KilledEnemies, Is.Zero);
            Assert.That(progress.CollectedRewardCount, Is.Zero);
        }

        [Test]
        public void CollectReward_WithInvalidGrant_Throws()
        {
            var progress = new DungeonRunProgress(enemyCount: 0);

            Assert.Throws<ArgumentException>(() => progress.CollectReward(default));
        }
    }
}

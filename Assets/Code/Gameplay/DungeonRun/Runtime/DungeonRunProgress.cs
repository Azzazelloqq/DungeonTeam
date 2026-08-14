using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunProgress
    {
        private readonly Dictionary<string, int> _collectedRewards =
            new(StringComparer.Ordinal);
        private bool _routeCompleted;

        public DungeonRunProgress(int enemyCount, bool requiresRouteCompletion = false)
        {
            if (enemyCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyCount));
            }

            RemainingEnemies = enemyCount;
            _routeCompleted = !requiresRouteCompletion;
        }

        public int KilledEnemies { get; private set; }

        public int RemainingEnemies { get; private set; }

        public int CollectedRewardCount { get; private set; }

        public DungeonRunOutcome? Outcome { get; private set; }

        public bool IsFinished => Outcome.HasValue;

        public bool CanExit => !IsFinished && RemainingEnemies == 0 && _routeCompleted;

        public bool RecordRouteCompleted()
        {
            if (IsFinished || _routeCompleted)
            {
                return false;
            }

            _routeCompleted = true;
            return true;
        }

        public bool RecordEnemyKilled()
        {
            if (IsFinished || RemainingEnemies == 0)
            {
                return false;
            }

            RemainingEnemies--;
            KilledEnemies++;
            return true;
        }

        public bool CollectReward(RewardGrant reward)
        {
            if (string.IsNullOrWhiteSpace(reward.RewardId) || reward.Amount <= 0)
            {
                throw new ArgumentException("Reward grant is invalid.", nameof(reward));
            }

            if (IsFinished)
            {
                return false;
            }

            _collectedRewards.TryGetValue(reward.RewardId, out var currentAmount);
            _collectedRewards[reward.RewardId] = checked(currentAmount + reward.Amount);
            CollectedRewardCount = checked(CollectedRewardCount + reward.Amount);
            return true;
        }

        public RewardGrant[] CreateCollectedRewardsSnapshot()
        {
            var rewardIds = new List<string>(_collectedRewards.Keys);
            rewardIds.Sort(StringComparer.Ordinal);
            var rewards = new RewardGrant[rewardIds.Count];
            for (var index = 0; index < rewardIds.Count; index++)
            {
                var rewardId = rewardIds[index];
                rewards[index] = new RewardGrant(rewardId, _collectedRewards[rewardId]);
            }

            return rewards;
        }

        public bool TryFinish(DungeonRunOutcome outcome)
        {
            if (IsFinished)
            {
                return false;
            }

            if (outcome == DungeonRunOutcome.Completed && !CanExit)
            {
                return false;
            }

            if (outcome is not DungeonRunOutcome.Completed and not DungeonRunOutcome.Defeated)
            {
                throw new ArgumentOutOfRangeException(nameof(outcome));
            }

            Outcome = outcome;
            return true;
        }
    }
}

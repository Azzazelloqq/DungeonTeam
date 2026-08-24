using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public enum DungeonRunOutcome
    {
        Completed,
        Defeated
    }

    public readonly struct DungeonRunResult
    {
        internal DungeonRunResult(
            DungeonRunOutcome outcome,
            string dungeonId,
            int seed,
            int killedEnemies,
            RewardGrant[] collectedRewards)
            : this(
                Guid.NewGuid().ToString("N"),
                outcome,
                dungeonId,
                seed,
                killedEnemies,
                collectedRewards)
        {
        }

        internal DungeonRunResult(
            string runId,
            DungeonRunOutcome outcome,
            string dungeonId,
            int seed,
            int killedEnemies,
            RewardGrant[] collectedRewards)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException("Run ID cannot be empty.", nameof(runId));
            }

            if (string.IsNullOrWhiteSpace(dungeonId))
            {
                throw new ArgumentException("Dungeon id cannot be empty.", nameof(dungeonId));
            }

            if (killedEnemies < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(killedEnemies));
            }

            if (collectedRewards == null)
            {
                throw new ArgumentNullException(nameof(collectedRewards));
            }

            RunId = runId;
            Outcome = outcome;
            DungeonId = dungeonId;
            Seed = seed;
            KilledEnemies = killedEnemies;
            CollectedRewards = Array.AsReadOnly((RewardGrant[])collectedRewards.Clone());
            var collectedRewardCount = 0;
            for (var index = 0; index < CollectedRewards.Count; index++)
            {
                collectedRewardCount = checked(
                    collectedRewardCount + CollectedRewards[index].Amount);
            }

            CollectedRewardCount = collectedRewardCount;
        }

        internal DungeonRunResult(
            DungeonRunOutcome outcome,
            string dungeonId,
            int seed,
            int killedEnemies,
            RewardGrant[] collectedRewards,
            string runId)
            : this(runId, outcome, dungeonId, seed, killedEnemies, collectedRewards)
        {
        }

        public DungeonRunOutcome Outcome { get; }

        public string RunId { get; }

        public string DungeonId { get; }

        public int Seed { get; }

        public int KilledEnemies { get; }

        public int CollectedRewardCount { get; }

        public IReadOnlyList<RewardGrant> CollectedRewards { get; }
    }
}

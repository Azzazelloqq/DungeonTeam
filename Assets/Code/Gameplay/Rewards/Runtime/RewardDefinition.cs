using System;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardDefinition
    {
        public RewardDefinition(string rewardId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID cannot be empty.", nameof(rewardId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Reward display name cannot be empty.", nameof(displayName));
            }

            RewardId = rewardId;
            DisplayName = displayName;
        }

        public string RewardId { get; }

        public string DisplayName { get; }

    }
}

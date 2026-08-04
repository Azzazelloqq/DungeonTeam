using System;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public readonly struct RewardGrant
    {
        public RewardGrant(string rewardId, int amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID cannot be empty.", nameof(rewardId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            RewardId = rewardId;
            Amount = amount;
        }

        public string RewardId { get; }

        public int Amount { get; }
    }
}

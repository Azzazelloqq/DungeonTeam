using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public readonly struct RewardPickupSpawnRequest
    {
        public RewardPickupSpawnRequest(
            Vector3 position,
            RewardDefinition definition,
            int amount)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Position = position;
            Amount = amount;
        }

        public Vector3 Position { get; }

        public RewardDefinition Definition { get; }

        public int Amount { get; }

        public RewardGrant Grant => new(Definition.RewardId, Amount);
    }
}

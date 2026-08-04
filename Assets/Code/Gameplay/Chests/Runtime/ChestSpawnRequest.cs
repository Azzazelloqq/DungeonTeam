using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public readonly struct ChestSpawnRequest
    {
        public ChestSpawnRequest(
            string instanceName,
            string rewardProfileId,
            Vector3 position,
            Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
            {
                throw new ArgumentException(
                    "Chest instance name cannot be empty.",
                    nameof(instanceName));
            }

            if (string.IsNullOrWhiteSpace(rewardProfileId))
            {
                throw new ArgumentException(
                    "Chest reward profile id cannot be empty.",
                    nameof(rewardProfileId));
            }

            InstanceName = instanceName;
            RewardProfileId = rewardProfileId;
            Position = position;
            Rotation = rotation;
        }

        public string InstanceName { get; }

        public string RewardProfileId { get; }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }
    }
}

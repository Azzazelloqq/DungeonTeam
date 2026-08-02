using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public readonly struct ActorSpawnRequest
    {
        public ActorSpawnRequest(
            string instanceName,
            Vector3 position,
            Quaternion rotation,
            int maximumHealth,
            float movementSpeed,
            Color color)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
            {
                throw new ArgumentException("Instance name cannot be empty.", nameof(instanceName));
            }

            if (maximumHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            }

            if (movementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            InstanceName = instanceName;
            Position = position;
            Rotation = rotation;
            MaximumHealth = maximumHealth;
            MovementSpeed = movementSpeed;
            Color = color;
        }

        public string InstanceName { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int MaximumHealth { get; }
        public float MovementSpeed { get; }
        public Color Color { get; }
    }
}

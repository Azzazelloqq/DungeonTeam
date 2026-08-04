using System;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorDefinition
    {
        public ActorDefinition(
            string actorId,
            ActorViewBase prefab,
            int maximumHealth,
            float movementSpeed)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            ActorId = actorId;
            Prefab = prefab != null
                ? prefab
                : throw new ArgumentNullException(nameof(prefab));
            MaximumHealth = maximumHealth > 0
                ? maximumHealth
                : throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            MovementSpeed = movementSpeed > 0f
                ? movementSpeed
                : throw new ArgumentOutOfRangeException(nameof(movementSpeed));
        }

        public string ActorId { get; }

        public ActorViewBase Prefab { get; }

        public int MaximumHealth { get; }

        public float MovementSpeed { get; }

    }
}

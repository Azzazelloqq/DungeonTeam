using System;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorDefinition
    {
        public ActorDefinition(
            string actorId,
            ActorViewBase prefab)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            ActorId = actorId;
            Prefab = prefab != null
                ? prefab
                : throw new ArgumentNullException(nameof(prefab));
        }

        public string ActorId { get; }

        public ActorViewBase Prefab { get; }

    }
}

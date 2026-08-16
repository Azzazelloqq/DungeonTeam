using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base
{
    public abstract class GuildHallModelBase : Model
    {
        public abstract bool IsWorldInputBlocked { get; }
        public abstract Vector3 Velocity { get; }
        public abstract string CurrentInteractionId { get; }

        public abstract void SetWorldInputBlocked(bool isBlocked);
        public abstract void SetVelocity(Vector3 velocity);
        public abstract void SetCurrentInteraction(string interactionId);
    }
}

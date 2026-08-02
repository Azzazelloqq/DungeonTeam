using DungeonTeam.Gameplay.Actors.Domain;
using MVP;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base
{
    public abstract class ActorModelBase : Model
    {
        public abstract int MaximumHealth { get; }

        public abstract int CurrentHealth { get; }

        public abstract bool IsAlive { get; }

        public abstract ActorDamageResult ApplyDamage(int amount);
    }
}

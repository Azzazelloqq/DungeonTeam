using DungeonTeam.Gameplay.Actors.Domain;
using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base
{
    public abstract class ActorPresenterBase : Presenter<ActorViewBase, ActorModelBase>
    {
        protected ActorPresenterBase(ActorViewBase view, ActorModelBase model)
            : base(view, model)
        {
        }

        public abstract Vector3 Position { get; }

        public abstract int CurrentHealth { get; }

        public abstract bool IsAlive { get; }

        public abstract bool TryMoveTo(Vector3 destination);

        public abstract bool SetMoveDirection(Vector3 direction);

        public abstract void StopMovement();

        public abstract ActorDamageResult ApplyDamage(int amount);
    }
}

using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base
{
    public abstract class ActorViewBase : ViewMonoBehaviour<ActorPresenterBase>
    {
        public abstract Vector3 Position { get; }

        public abstract bool IsOnNavMesh { get; }

        public abstract void Configure(Color color, float movementSpeed);

        public abstract bool TryMoveTo(Vector3 destination);

        public abstract bool SetMoveDirection(Vector3 direction);

        public abstract void StopMovement();

        public abstract void ShowDead();
    }
}

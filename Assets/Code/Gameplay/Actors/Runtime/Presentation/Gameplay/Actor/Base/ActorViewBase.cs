using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base
{
    public abstract class ActorViewBase : ViewMonoBehaviour<ActorPresenterBase>
    {
        public abstract Vector3 Position { get; }

        public abstract Vector3 Forward { get; }

        public abstract bool IsOnNavMesh { get; }

        public abstract Transform WeaponAnchor { get; }

        public abstract Transform HitVfxAnchor { get; }

        public abstract Transform OverheadAnchor { get; }

        public abstract Transform SkillOriginAnchor { get; }

        public abstract void Configure(float movementSpeed);

        public abstract bool TryMoveTo(Vector3 destination);

        public abstract bool SetMoveDirection(Vector3 direction);

        public abstract bool TryFaceTowards(Vector3 targetPosition);

        public abstract void StopMovement();

        public abstract void PlayAttackFeedback();

        public abstract void PlayCastFeedback();

        public virtual void CancelActionFeedback()
        {
        }

        public abstract void PlayDamageFeedback(int amount, bool isFatal);

        public abstract void PlayDeathFeedback();
    }
}

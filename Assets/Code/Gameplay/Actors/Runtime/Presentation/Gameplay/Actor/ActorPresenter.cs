using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    public sealed class ActorPresenter : ActorPresenterBase
    {
        private readonly float _movementSpeed;

        public ActorPresenter(
            ActorViewBase view,
            ActorModelBase model,
            float movementSpeed)
            : base(view, model)
        {
            _movementSpeed = movementSpeed;
        }

        public override Vector3 Position => view.Position;

        public override Vector3 Forward => view.Forward;

        public override int CurrentHealth => model.CurrentHealth;

        public override int MaximumHealth => model.MaximumHealth;

        public override bool IsAlive => model.IsAlive;

        public override Transform WeaponAnchor => view.WeaponAnchor;

        public override Transform HitVfxAnchor => view.HitVfxAnchor;

        public override Transform OverheadAnchor => view.OverheadAnchor;

        public override Transform SkillOriginAnchor => view.SkillOriginAnchor;

        public override bool TryMoveTo(Vector3 destination)
        {
            return model.IsAlive && view.TryMoveTo(destination);
        }

        public override bool SetMoveDirection(Vector3 direction)
        {
            return model.IsAlive && view.SetMoveDirection(direction);
        }

        public override bool TryFaceTowards(Vector3 targetPosition)
        {
            return model.IsAlive && view.TryFaceTowards(targetPosition);
        }

        public override void StopMovement()
        {
            view.StopMovement();
        }

        public override void PlayAttackFeedback()
        {
            if (model.IsAlive)
            {
                view.PlayAttackFeedback();
            }
        }

        public override void PlayCastFeedback()
        {
            if (model.IsAlive)
            {
                view.PlayCastFeedback();
            }
        }

        public override ActorDamageResult ApplyDamage(int amount)
        {
            var result = model.ApplyDamage(amount);
            if (result != ActorDamageResult.Ignored)
            {
                view.PlayDamageFeedback(amount, result == ActorDamageResult.Killed);
            }

            if (result == ActorDamageResult.Killed)
            {
                view.PlayDeathFeedback();
            }

            return result;
        }

        public override ActorHealResult ApplyHeal(int amount)
        {
            return model.ApplyHeal(amount);
        }

        protected override void OnInitialize()
        {
            view.Configure(_movementSpeed);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    public sealed class ActorPresenter : ActorPresenterBase
    {
        private readonly Color _color;
        private readonly float _movementSpeed;

        public ActorPresenter(
            ActorViewBase view,
            ActorModelBase model,
            Color color,
            float movementSpeed)
            : base(view, model)
        {
            _color = color;
            _movementSpeed = movementSpeed;
        }

        public override Vector3 Position => view.Position;

        public override int CurrentHealth => model.CurrentHealth;

        public override bool IsAlive => model.IsAlive;

        public override bool TryMoveTo(Vector3 destination)
        {
            return model.IsAlive && view.TryMoveTo(destination);
        }

        public override bool SetMoveDirection(Vector3 direction)
        {
            return model.IsAlive && view.SetMoveDirection(direction);
        }

        public override void StopMovement()
        {
            view.StopMovement();
        }

        public override ActorDamageResult ApplyDamage(int amount)
        {
            var result = model.ApplyDamage(amount);
            if (result == ActorDamageResult.Killed)
            {
                view.ShowDead();
            }

            return result;
        }

        protected override void OnInitialize()
        {
            view.Configure(_color, _movementSpeed);
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

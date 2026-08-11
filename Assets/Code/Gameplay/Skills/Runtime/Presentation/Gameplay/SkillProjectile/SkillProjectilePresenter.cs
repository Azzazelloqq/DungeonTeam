using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile
{
    public sealed class SkillProjectilePresenter : SkillProjectilePresenterBase
    {
        private const float HitDistance = 0.15f;
        private readonly Action<Vector3> _onImpact;

        public SkillProjectilePresenter(
            SkillProjectileViewBase view,
            SkillProjectileModelBase model,
            ActorInstance source,
            ActorInstance target,
            Action<Vector3> onImpact)
            : base(view, model)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            _onImpact = onImpact ?? throw new ArgumentNullException(nameof(onImpact));
        }

        public override bool IsCompleted => model.IsCompleted;
        public override ActorInstance Source { get; }
        public override ActorInstance Target { get; }

        public override void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (model.IsCompleted)
                return;
            if (!Target.IsAlive)
            {
                model.TryComplete();
                return;
            }

            var targetPosition = Target.HitVfxAnchor != null
                ? Target.HitVfxAnchor.position
                : Target.Position + Vector3.up;
            var difference = targetPosition - view.Position;
            var travelDistance = model.Speed * deltaTime;
            if (difference.sqrMagnitude <=
                (travelDistance + HitDistance) * (travelDistance + HitDistance))
            {
                if (model.TryComplete())
                {
                    Target.ApplyDamage(model.Damage, Source);
                    _onImpact(targetPosition);
                }

                return;
            }

            view.Position += difference.normalized * travelDistance;
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile
{
    public sealed class SkillProjectileView : SkillProjectileViewBase
    {
        public override Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile
{
    public sealed class SkillProjectileModel : SkillProjectileModelBase
    {
        private bool _isCompleted;

        public SkillProjectileModel(int damage, float speed)
        {
            Damage = damage > 0
                ? damage
                : throw new ArgumentOutOfRangeException(nameof(damage));
            Speed = speed > 0f
                ? speed
                : throw new ArgumentOutOfRangeException(nameof(speed));
        }

        public override int Damage { get; }
        public override float Speed { get; }
        public override bool IsCompleted => _isCompleted;

        public override bool TryComplete()
        {
            if (_isCompleted)
                return false;

            _isCompleted = true;
            return true;
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

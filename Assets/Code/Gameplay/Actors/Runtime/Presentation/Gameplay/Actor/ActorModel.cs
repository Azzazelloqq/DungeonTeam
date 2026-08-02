using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    public sealed class ActorModel : ActorModelBase
    {
        private readonly ActorHealth _health;

        public ActorModel(int maximumHealth)
        {
            _health = new ActorHealth(maximumHealth);
        }

        public override int MaximumHealth => _health.Maximum;

        public override int CurrentHealth => _health.Current;

        public override bool IsAlive => _health.IsAlive;

        public override ActorDamageResult ApplyDamage(int amount)
        {
            return _health.ApplyDamage(amount);
        }

        protected override void OnInitialize()
        {
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

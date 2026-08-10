using MVP;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base
{
    public abstract class SkillProjectileModelBase : Model
    {
        public abstract int Damage { get; }
        public abstract float Speed { get; }
        public abstract bool IsCompleted { get; }
        public abstract bool TryComplete();
    }
}

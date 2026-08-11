using DungeonTeam.Gameplay.Actors.Runtime;
using MVP;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base
{
    public abstract class SkillProjectilePresenterBase :
        Presenter<SkillProjectileViewBase, SkillProjectileModelBase>
    {
        protected SkillProjectilePresenterBase(
            SkillProjectileViewBase view,
            SkillProjectileModelBase model)
            : base(view, model)
        {
        }

        public abstract bool IsCompleted { get; }
        public abstract ActorInstance Source { get; }
        public abstract ActorInstance Target { get; }
        public abstract void Tick(float deltaTime);
    }
}

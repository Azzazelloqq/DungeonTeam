using Code.EnemiesCore.Enemies.Base;
using MVP;

namespace Code.EnemiesCore.Enemies.GoblinEnemy.BaseMVP
{
public abstract class GoblinEnemyPresenterBase : Presenter<GoblinEnemyViewBase, GoblinEnemyModelBase>, IEnemy
{
    protected GoblinEnemyPresenterBase(GoblinEnemyViewBase view, GoblinEnemyModelBase model) : base(view, model)
    {
    }
}
}
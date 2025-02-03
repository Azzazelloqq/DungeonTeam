using MVP;

namespace Code.EnemiesCore.Enemies.Base.BaseEnemy
{
public abstract class EnemyPresenterBase : Presenter<EnemyViewBase, EnemyModelBase>, IEnemy
{
    protected EnemyPresenterBase(EnemyViewBase view, EnemyModelBase model) : base(view, model)
    {
    }
}
}
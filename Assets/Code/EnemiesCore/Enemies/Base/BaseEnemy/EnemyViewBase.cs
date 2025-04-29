using MVP;

namespace Code.EnemiesCore.Enemies.Base.BaseEnemy
{
public abstract class EnemyViewBase : ViewMonoBehaviour<EnemyPresenterBase>
{
	public abstract void TakeCommonAttackDamage();
	public abstract void StartDieEffect();
}
}
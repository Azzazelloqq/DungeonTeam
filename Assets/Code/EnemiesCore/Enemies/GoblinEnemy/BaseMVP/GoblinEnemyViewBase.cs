using MVP;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.GoblinEnemy.BaseMVP
{
public abstract class GoblinEnemyViewBase : ViewMonoBehaviour<GoblinEnemyPresenterBase>
{
    public abstract void TakeCommonAttackDamage();
    public abstract void TakeFireballDamage();
    public abstract void StartDieEffect();
}
}
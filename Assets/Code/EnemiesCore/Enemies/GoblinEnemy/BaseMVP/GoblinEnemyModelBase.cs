using MVP;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.GoblinEnemy.BaseMVP
{
public abstract class GoblinEnemyModelBase : Model
{
    public abstract bool IsDead { get; protected set; }
    public abstract void TakeCommonAttackDamage(int damage);
    public abstract void TakeFireballDamage(int damage);
}
}
using MVP;

namespace Code.EnemiesCore.Enemies.Base.BaseEnemy
{
public abstract class EnemyModelBase : Model
{
    public abstract bool IsDead { get; protected set; }
    public abstract void TakeCommonAttackDamage(int damage);
    public abstract void TakeFireballDamage(int damage);
}
}
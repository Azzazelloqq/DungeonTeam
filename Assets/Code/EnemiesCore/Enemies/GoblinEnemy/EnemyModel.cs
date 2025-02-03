using Code.EnemiesCore.Enemies.Base.BaseEnemy;

namespace Code.EnemiesCore.Enemies.GoblinEnemy
{
public class EnemyModel : EnemyModelBase
{
    public override bool IsDead { get; protected set; }
    
    private int _currentHealth;

    public EnemyModel(int health)
    {
        _currentHealth = health;
    }
    
    public override void TakeCommonAttackDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        _currentHealth -= damage;

        IsDead = _currentHealth <= 0;
    }

    public override void TakeFireballDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        _currentHealth -= damage;

        IsDead = _currentHealth <= 0;    }
}
}
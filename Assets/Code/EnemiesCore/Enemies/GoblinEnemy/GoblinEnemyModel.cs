using Code.EnemiesCore.Enemies.GoblinEnemy.BaseMVP;

namespace Code.EnemiesCore.Enemies.GoblinEnemy
{
public class GoblinEnemyModel : GoblinEnemyModelBase
{
    public override bool IsDead { get; protected set; }
    
    private int _currentHealth;

    public GoblinEnemyModel(int health)
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
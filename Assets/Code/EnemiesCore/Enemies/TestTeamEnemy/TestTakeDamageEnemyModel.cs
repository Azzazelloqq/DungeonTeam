using Code.EnemiesCore.Enemies.Base.BaseEnemy;

namespace Code.EnemiesCore.Enemies.TestTeamEnemy
{
public class TestTakeDamageEnemyModel : EnemyModelBase
{
	public override bool IsDead { get; protected set; }
	public override void TakeCommonAttackDamage(int damage)
	{
	}

	public override void TakeFireballDamage(int damage)
	{
	}
}
}
using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.AttackAgents
{
public interface IAttackEnemyAgent : IBehaviourTreeAgent
{
	public bool IsAttackCasting { get; }
	public bool CanStartAttack { get; }

	public void AttackEnemy();
}
}
using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class AttackEnemyNode : IBehaviourTreeNode
{
	private readonly IAttackEnemyAgent _agent;

	public AttackEnemyNode(IAttackEnemyAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.AttackEnemy();

		return NodeState.Success;
	}

	public void Dispose()
	{
	}
}
}
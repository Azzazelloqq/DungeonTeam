using Code.AI.CharacterBehaviourTree.Agents.AttackAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.AttackNodes
{
public class IsAvailableAttackEnemyNode : IBehaviourTreeNode
{
	private readonly IAttackEnemyAgent _agent;

	public IsAvailableAttackEnemyNode(IAttackEnemyAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.CanStartAttack
			? NodeState.Success
			: NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
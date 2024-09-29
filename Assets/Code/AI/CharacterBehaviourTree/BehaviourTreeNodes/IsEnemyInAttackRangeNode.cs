using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class IsEnemyInAttackRangeNode : IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public IsEnemyInAttackRangeNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.IsEnemyInAttackRange() ? NodeState.Success : NodeState.Failure;
	}
}
}
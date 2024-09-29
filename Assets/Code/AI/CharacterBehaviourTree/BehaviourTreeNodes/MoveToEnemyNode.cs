using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class MoveToEnemyNode : IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public MoveToEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.MoveToEnemy();

		return NodeState.Running;
	}
}
}
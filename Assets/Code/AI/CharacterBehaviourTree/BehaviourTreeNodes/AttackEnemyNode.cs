using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class AttackEnemyNode : IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public AttackEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.AttackEnemy();

		return NodeState.Success;
	}
}
}
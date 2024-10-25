using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class MoveToEnemyNode : IBehaviourTreeNode
{
	private readonly IMoveToEnemyAgent _agent;

	public MoveToEnemyNode(IMoveToEnemyAgent agent)
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
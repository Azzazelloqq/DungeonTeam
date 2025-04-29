using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
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
		if (_agent.IsEnemyReached)
		{
			return NodeState.Success;
		}

		_agent.MoveToEnemyForAttack();

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
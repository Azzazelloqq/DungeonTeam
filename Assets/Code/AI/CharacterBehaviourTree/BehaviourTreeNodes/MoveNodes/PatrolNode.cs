using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class PatrolNode : IBehaviourTreeNode
{
	private readonly IPatrolAgent _agent;

	public PatrolNode(IPatrolAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.Patrol();

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
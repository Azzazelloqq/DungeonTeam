using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class StopMovementNode : IBehaviourTreeNode
{
	private readonly IMoveAgent _agent;

	public StopMovementNode(IMoveAgent agent)
	{
		_agent = agent;
	}
	
	public NodeState Tick()
	{
		_agent.StopMovement();

		return NodeState.Success;
	}

	public void Dispose()
	{
	}
}
}

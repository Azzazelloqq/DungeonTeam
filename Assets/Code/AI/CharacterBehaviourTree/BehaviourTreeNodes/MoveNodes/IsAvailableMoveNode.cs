using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class IsAvailableMoveNode : IBehaviourTreeNode
{
	private readonly IMoveAgent _agent;

	public IsAvailableMoveNode(IMoveAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		var isCanMove = _agent.IsCanMove;

		return isCanMove ? NodeState.Success : NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
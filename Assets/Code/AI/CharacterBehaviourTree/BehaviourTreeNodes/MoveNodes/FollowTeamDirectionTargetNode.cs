using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class FollowTeamDirectionTargetNode : IBehaviourTreeNode
{
	private readonly IFollowToTeamDirectionAgent _agent;

	public FollowTeamDirectionTargetNode(IFollowToTeamDirectionAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		var isNeedFollowToDirection = _agent.IsNeedFollowToDirection();

		if (!isNeedFollowToDirection)
		{
			return NodeState.Failure;
		}

		_agent.FollowToDirection();

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
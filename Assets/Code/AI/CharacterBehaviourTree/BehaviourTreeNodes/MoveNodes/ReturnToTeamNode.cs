using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class ReturnToTeamNode : IBehaviourTreeNode
{
	private readonly IFollowToTeamDirectionAgent _agent;

	private bool _isGoingToTeamPlace;

	public ReturnToTeamNode(IFollowToTeamDirectionAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_agent.IsOnTeamPlace)
		{
			_isGoingToTeamPlace = false;

			return NodeState.Success;
		}

		if (!_isGoingToTeamPlace)
		{
			_agent.ReturnToTeam();
			_isGoingToTeamPlace = true;
		}

		if (_isGoingToTeamPlace)
		{
			return NodeState.Running;
		}

		return NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class ReturnToTeamNode : IBehaviourTreeNode
{
	private readonly IFollowToTeamDirectionAgent _agent;


	public ReturnToTeamNode(IFollowToTeamDirectionAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_agent.IsOnTeamPlace)
		{
			return NodeState.Success;
		}

		_agent.ReturnToTeam();

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
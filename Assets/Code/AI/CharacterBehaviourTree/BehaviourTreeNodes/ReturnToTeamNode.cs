using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
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
		_agent.ReturnToTeam();
		
		return _agent.IsOnTeamPlace ? NodeState.Success : NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
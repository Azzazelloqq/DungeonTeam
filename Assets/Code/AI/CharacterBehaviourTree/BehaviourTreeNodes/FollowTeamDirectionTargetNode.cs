using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class FollowTeamDirectionTargetNode : IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public FollowTeamDirectionTargetNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.FollowToDirection();

		return NodeState.Running;
	}
}
}
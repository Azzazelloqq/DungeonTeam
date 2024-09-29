using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class UseSupportSkillNode: IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public UseSupportSkillNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.UseSupportSkill();

		return NodeState.Success;
	}
}
}
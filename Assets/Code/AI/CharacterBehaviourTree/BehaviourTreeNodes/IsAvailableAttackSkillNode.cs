using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class IsAvailableAttackSkillNode : IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public IsAvailableAttackSkillNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.IsAvailableAttackSkill() ? NodeState.Success : NodeState.Failure;
	}
}
}
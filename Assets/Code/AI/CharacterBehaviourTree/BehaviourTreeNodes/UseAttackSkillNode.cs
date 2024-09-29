using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class UseAttackSkillNode: IBehaviourTreeNode
{
	private readonly ICharacterBehaviourTreeAgent _agent;

	public UseAttackSkillNode(ICharacterBehaviourTreeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.UseSkill();
		
		return NodeState.Success;
	}
}
}
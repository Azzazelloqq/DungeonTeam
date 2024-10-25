using Code.AI.CharacterBehaviourTree.Agents;
using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class IsAvailableUseAttackSkillNode : IBehaviourTreeNode
{
	private readonly IAvailableUseAttackSkillAgent _agent;

	public IsAvailableUseAttackSkillNode(IAvailableUseAttackSkillAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.IsAvailableUseAttackSkill() ? NodeState.Success : NodeState.Failure;
	}
}
}
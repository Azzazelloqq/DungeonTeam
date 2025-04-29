using Code.AI.CharacterBehaviourTree.Agents.SkillsAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.SkillsNodes
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

	public void Dispose()
	{
	}
}
}
using Code.AI.CharacterBehaviourTree.Agents.SkillsAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.SkillsNodes
{
public class UseHealSkillNode: IBehaviourTreeNode
{
	private readonly IUseHealSkillAgent _agent;

	public UseHealSkillNode(IUseHealSkillAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.UseHealSkill();

		return NodeState.Success;
	}

	public void Dispose()
	{
	}
}
}
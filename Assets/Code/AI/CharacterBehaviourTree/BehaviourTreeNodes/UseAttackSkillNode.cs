using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class UseAttackSkillNode: IBehaviourTreeNode
{
	private readonly IUseAttackSkillAgent _agent;

	public UseAttackSkillNode(IUseAttackSkillAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		_agent.UseAttackSkill();
		
		return NodeState.Success;
	}

	public void Dispose()
	{
	}
}
}
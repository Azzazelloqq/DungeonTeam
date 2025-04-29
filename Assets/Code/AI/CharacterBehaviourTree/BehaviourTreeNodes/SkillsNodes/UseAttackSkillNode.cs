using Code.AI.CharacterBehaviourTree.Agents.SkillsAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.SkillsNodes
{
public class UseAttackSkillNode : IBehaviourTreeNode
{
	private readonly IUseAttackSkillAgent _agent;
	private bool _castStarted;

	public UseAttackSkillNode(IUseAttackSkillAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_castStarted)
		{
			if (_agent.IsAttackSkillCasting)
			{
				return NodeState.Running;
			}

			_castStarted = false;
			return NodeState.Success;
		}

		if (_agent.CanStartAttackSkill)
		{
			_agent.UseAttackSkill();
			_castStarted = true;
			return NodeState.Running;
		}

		return NodeState.Success;
	}

	public void Dispose()
	{
	}
}
}
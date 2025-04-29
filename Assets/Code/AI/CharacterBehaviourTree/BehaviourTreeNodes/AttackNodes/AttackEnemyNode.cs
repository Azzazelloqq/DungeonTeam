using Code.AI.CharacterBehaviourTree.Agents.AttackAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.AttackNodes
{
public class AttackEnemyNode : IBehaviourTreeNode
{
	private readonly IAttackEnemyAgent _agent;
	private bool _attackStarted;

	public AttackEnemyNode(IAttackEnemyAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_attackStarted)
		{
			if (_agent.IsAttackCasting)
			{
				return NodeState.Running;
			}

			_attackStarted = false;
			return NodeState.Success;
		}

		_agent.AttackEnemy();
		_attackStarted = true;

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
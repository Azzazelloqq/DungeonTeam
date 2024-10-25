using Code.AI.CharacterBehaviourTree.Agents;
using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class IsEnemyInAttackRangeNode : IBehaviourTreeNode
{
	private readonly ITrackEnemyInAttackRangeAgent _agent;

	public IsEnemyInAttackRangeNode(ITrackEnemyInAttackRangeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.IsEnemyInAttackRange() ? NodeState.Success : NodeState.Failure;
	}
}
}
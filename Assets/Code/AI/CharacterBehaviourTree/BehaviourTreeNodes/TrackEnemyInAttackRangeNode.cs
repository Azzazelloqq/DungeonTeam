using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class TrackEnemyInAttackRangeNode : IBehaviourTreeNode
{
	private readonly ITrackEnemyInAttackRangeAgent _agent;

	public TrackEnemyInAttackRangeNode(ITrackEnemyInAttackRangeAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.IsEnemyInAttackRange() ? NodeState.Success : NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
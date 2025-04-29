using Code.AI.CharacterBehaviourTree.Agents.TrackAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.TrackNodes
{
public class FindEnemyTargetInSightNode : IBehaviourTreeNode
{
	private readonly IFindEnemyTargetAgent _agent;

	public FindEnemyTargetInSightNode(IFindEnemyTargetAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_agent.TryFindEnemyTarget())
		{
			return NodeState.Success;
		}

		return NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
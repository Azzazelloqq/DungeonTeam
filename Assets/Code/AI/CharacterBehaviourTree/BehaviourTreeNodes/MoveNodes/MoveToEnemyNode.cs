using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes
{
public class MoveToEnemyNode : IBehaviourTreeNode
{
	private readonly IMoveToEnemyAgent _agent;

	private bool _isMovingToTarget;
	
	public MoveToEnemyNode(IMoveToEnemyAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		if (_agent.IsEnemyReached)
		{
			return NodeState.Success;
		}

		if (_isMovingToTarget)
		{
			return NodeState.Running;
		}

		_agent.MoveToEnemyForAttack();
		_isMovingToTarget = true;
			
		return NodeState.Running;
	}

	public void Dispose()
	{
		_isMovingToTarget = false;
	}
}
}
using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class PatrolNode : IBehaviourTreeNode
{
    private readonly IPatrolAgent _agent;
    
    public PatrolNode(IPatrolAgent agent)
    {
        _agent = agent;
    }
    
    public NodeState Tick()
    {
        _agent.Patrol();

        return NodeState.Running;
    }

    public void Dispose()
    {
    }
}
}
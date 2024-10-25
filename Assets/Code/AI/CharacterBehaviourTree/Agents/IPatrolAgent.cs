using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface IPatrolAgent : IBehaviourTreeAgent
{
    public void Patrol();
}
}
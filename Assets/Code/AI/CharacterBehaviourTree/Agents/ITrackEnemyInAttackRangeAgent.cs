using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface ITrackEnemyInAttackRangeAgent : IBehaviourTreeAgent
{
    public bool IsEnemyInAttackRange();
}
}
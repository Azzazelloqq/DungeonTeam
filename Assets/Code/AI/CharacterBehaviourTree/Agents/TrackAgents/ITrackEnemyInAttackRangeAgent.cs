using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.TrackAgents
{
public interface ITrackEnemyInAttackRangeAgent : IBehaviourTreeAgent
{
	public bool IsEnemyInAttackRange();
}
}
using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.TrackAgents
{
public interface ITrackNeedSupportAgent : IBehaviourTreeAgent
{
	public bool TryFindTargetToHeal();
}
}
using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface IFindEnemyTargetAgent : IBehaviourTreeAgent
{
	public bool TryFindEnemyTarget();
}
}
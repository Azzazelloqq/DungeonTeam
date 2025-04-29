using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base
{
public interface IMoveAgent : IBehaviourTreeAgent
{
	public bool IsCanMove { get; }
}
}
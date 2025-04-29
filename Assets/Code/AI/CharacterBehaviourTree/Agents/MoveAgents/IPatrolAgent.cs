using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.MoveAgents
{
public interface IPatrolAgent : IMoveAgent
{
	public void Patrol();
}
}
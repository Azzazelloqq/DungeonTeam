using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.MoveAgents
{
public interface IMoveToEnemyAgent : IMoveAgent
{
	public bool IsEnemyReached { get; }
	public void MoveToEnemyForAttack();
}
}
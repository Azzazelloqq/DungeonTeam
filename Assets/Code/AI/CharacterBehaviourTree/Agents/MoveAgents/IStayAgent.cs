using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.MoveAgents
{
public interface IStayAgent : IMoveAgent
{
	public void MoveToStayPlace();
}
}
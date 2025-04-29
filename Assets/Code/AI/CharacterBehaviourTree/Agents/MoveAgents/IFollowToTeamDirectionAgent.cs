using Code.AI.CharacterBehaviourTree.Agents.MoveAgents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.MoveAgents
{
public interface IFollowToTeamDirectionAgent : IMoveAgent
{
	public bool IsOnTeamPlace { get; }

	public bool IsNeedFollowToDirection();
	public void FollowToDirection();
	public void ReturnToTeam();
}
}
using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface IFollowToTeamDirectionAgent : IBehaviourTreeAgent
{
	public bool IsNeedFollowToDirection();
	public void FollowToDirection();
	public void ReturnToTeam();
}
}
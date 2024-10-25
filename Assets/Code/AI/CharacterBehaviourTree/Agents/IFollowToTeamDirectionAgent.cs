using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface IFollowToTeamDirectionAgent : IBehaviourTreeAgent
{
    public void FollowToDirection();
	public bool IsNeedFollowToDirection();
}
}
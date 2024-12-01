using Code.AI.CharacterBehaviourTree.Agents;

namespace Code.AI.CharacterBehaviourTree.Trees.Character
{
public interface ICharacterBehaviourTreeAgent : 
    IAttackEnemyAgent,
    IAvailableUseAttackSkillAgent,
    ITrackEnemyInAttackRangeAgent,
    ITrackNeedSupportAgent,
    IFollowToTeamDirectionAgent,
    IMoveToEnemyAgent,
    IUseAttackSkillAgent,
    IUseHealSkillAgent,
	IFindEnemyTargetAgent,
	IStayAgent
{
}
}
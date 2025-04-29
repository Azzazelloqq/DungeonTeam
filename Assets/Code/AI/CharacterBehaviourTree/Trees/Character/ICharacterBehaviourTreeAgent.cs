using Code.AI.CharacterBehaviourTree.Agents.AttackAgents;
using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.AI.CharacterBehaviourTree.Agents.SkillsAgents;
using Code.AI.CharacterBehaviourTree.Agents.TrackAgents;

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
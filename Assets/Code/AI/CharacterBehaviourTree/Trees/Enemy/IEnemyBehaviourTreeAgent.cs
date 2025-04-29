using Code.AI.CharacterBehaviourTree.Agents.AttackAgents;
using Code.AI.CharacterBehaviourTree.Agents.MoveAgents;
using Code.AI.CharacterBehaviourTree.Agents.SkillsAgents;
using Code.AI.CharacterBehaviourTree.Agents.TrackAgents;

namespace Code.AI.CharacterBehaviourTree.Trees.Enemy
{
public interface IEnemyBehaviourTreeAgent :
	IAvailableUseAttackSkillAgent,
	IAttackEnemyAgent,
	ITrackEnemyInAttackRangeAgent,
	IMoveToEnemyAgent,
	IUseAttackSkillAgent,
	IFindEnemyTargetAgent,
	IPatrolAgent
{
}
}
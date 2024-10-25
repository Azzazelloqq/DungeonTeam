using Code.AI.CharacterBehaviourTree.Agents;

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
using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.SkillsAgents
{
public interface IAvailableUseAttackSkillAgent : IBehaviourTreeAgent
{
	public bool IsAvailableUseAttackSkill();
}
}
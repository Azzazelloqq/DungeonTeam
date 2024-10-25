using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface IAvailableUseAttackSkillAgent : IBehaviourTreeAgent
{
    public bool IsAvailableUseAttackSkill();
}
}
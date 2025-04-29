using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents.SkillsAgents
{
public interface IUseAttackSkillAgent : IBehaviourTreeAgent
{
    public bool IsAttackSkillCasting { get; }
    public bool CanStartAttackSkill { get; }
    public void UseAttackSkill();
}
}
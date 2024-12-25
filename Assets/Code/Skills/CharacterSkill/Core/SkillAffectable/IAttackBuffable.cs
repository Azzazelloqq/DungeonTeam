using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable
{
public interface IAttackBuffable : ISkillAffectable
{
	public void BuffAttack(int buffPercent);
	public void RemoveAttackBuff(int buffPercent);
}
}
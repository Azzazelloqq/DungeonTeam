using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable
{
public interface IMeleeAttackable : ISkillAffectable
{
	public void TakeCommonAttackDamage(int damage);
}
}
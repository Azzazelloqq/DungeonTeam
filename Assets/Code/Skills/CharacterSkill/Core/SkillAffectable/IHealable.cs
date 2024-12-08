using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable
{
public interface IHealable : ISkillAffectable
{
	public bool IsNeedHeal { get; }
	public void Heal(int healPoints);
}
}
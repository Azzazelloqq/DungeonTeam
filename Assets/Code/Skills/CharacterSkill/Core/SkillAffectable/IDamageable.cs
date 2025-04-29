using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable
{
public interface IDamageable : ISkillAffectable
{
	public void TakeDamage(int damage);
}
}
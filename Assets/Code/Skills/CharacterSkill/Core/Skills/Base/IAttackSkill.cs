using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.Skills.Base
{
public interface IAttackSkill<in TAffectable> : ISkill<TAffectable> where TAffectable : ISkillAffectable
{
	
}
}
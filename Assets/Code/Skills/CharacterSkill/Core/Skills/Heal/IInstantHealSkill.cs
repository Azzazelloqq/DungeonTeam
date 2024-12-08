using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.Skills.Base;

namespace Code.Skills.CharacterSkill.Core.Skills.Heal
{
public interface IInstantHealSkill : ISkill<IHealable>
{
	public int HealPoints { get; }
	
}
}
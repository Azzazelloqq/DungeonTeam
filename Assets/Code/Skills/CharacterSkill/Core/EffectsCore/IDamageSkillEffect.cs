using Code.Skills.CharacterSkill.Core.EffectsCore.Base;

namespace Code.Skills.CharacterSkill.Core.EffectsCore
{
public interface IDamageSkillEffect : ISkillEffect
{
	public int DamageAmount { get; }
}
}
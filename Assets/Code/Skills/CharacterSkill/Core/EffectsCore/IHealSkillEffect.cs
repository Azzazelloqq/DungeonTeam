using Code.Skills.CharacterSkill.Core.EffectsCore.Base;

namespace Code.Skills.CharacterSkill.Core.EffectsCore
{
public interface IHealSkillEffect : ISkillEffect
{
	public int TotalHealAmount { get; }
}
}
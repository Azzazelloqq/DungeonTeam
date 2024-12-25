using Code.Skills.CharacterSkill.Core.EffectsCore.Base;

namespace Code.Skills.CharacterSkill.Core.EffectsCore
{
public interface IBuffSkillEffect : ISkillEffect
{
	public int BuffDuration { get; }
	public int BuffAmount { get; }
}
}
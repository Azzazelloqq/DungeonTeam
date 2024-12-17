using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.EffectsCore.Base
{
/// <summary>
/// Represents an effect that can be applied to a target.
/// Effects are composable and allow flexible skill design.
/// </summary>
public interface ISkillEffect : IDisposable
{
	public event Action EffectApplied;
	
	/// <summary>
	/// Attempts to apply this effect to the given target.
	/// </summary>
	/// <param name="target">The target that might be affected.</param>
	public bool TryApplyEffect(ISkillAffectable target);
}
}
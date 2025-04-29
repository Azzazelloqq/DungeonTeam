using System;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Effects.Damage
{
public class DamageSkillEffect : IDamageSkillEffect
{
	public event Action EffectApplied;

	public string EffectId { get; }
	public int TotalDamageAmount { get; }

	public DamageSkillEffect(string effectId, int totalDamageAmount)
	{
		EffectId = effectId;
		TotalDamageAmount = totalDamageAmount;
	}

	public void Dispose()
	{
		EffectApplied = null;
	}

	public bool TryApplyEffect(ISkillAffectable target)
	{
		if (target.IsDead)
		{
			return false;
		}

		if (target is not IDamageable damageable)
		{
			return false;
		}

		damageable.TakeDamage(TotalDamageAmount);
		EffectApplied?.Invoke();

		return true;
	}
}
}
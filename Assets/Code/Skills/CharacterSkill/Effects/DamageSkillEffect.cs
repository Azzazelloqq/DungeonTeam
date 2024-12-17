using System;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Effects
{
public class DamageSkillEffect : IDamageSkillEffect
{
	public event Action EffectApplied;
	
	public int DamageAmount { get; }

	public DamageSkillEffect(int damageAmount)
	{
		DamageAmount = damageAmount;
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
		
		if (target is IDamageable damageable)
		{
			damageable.TakeDamage(DamageAmount);
			EffectApplied?.Invoke();
			
			return true;
		}

		return false;
	}
}
}
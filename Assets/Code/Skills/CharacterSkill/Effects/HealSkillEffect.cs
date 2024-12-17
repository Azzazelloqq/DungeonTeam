using System;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Effects
{
public class HealSkillEffect : IHealSkillEffect
{
	public event Action EffectApplied;
	
	public int HealAmount { get; }

	public HealSkillEffect(int healAmount)
	{
		HealAmount = healAmount;
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
		
		if (target is IHealable healable)
		{
			healable.Heal(HealAmount);
			EffectApplied?.Invoke();
			
			return true;
		}

		return false;
	}
}
}
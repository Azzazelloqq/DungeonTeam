using System;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Effects.Heal
{
public class HealSkillEffect : IHealSkillEffect
{
	public event Action EffectApplied;
	
	public string EffectId { get; }
	public int TotalHealAmount { get; }

	public HealSkillEffect(string effectId, int totalHealAmount)
	{
		EffectId = effectId;
		TotalHealAmount = totalHealAmount;
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
			healable.Heal(TotalHealAmount);
			EffectApplied?.Invoke();
			
			return true;
		}

		return false;
	}
}
}
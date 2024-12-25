using Code.GameConfig.ScriptableObjectParser.ConfigData.Effect;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Effects;
using Code.Skills.CharacterSkill.Effects.Buff;
using Code.Skills.CharacterSkill.Effects.Heal;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Factory.Effects
{
internal class SkillEffectsFactory : ISkillEffectsFactory
{
	private readonly IInGameLogger _logger;

	internal SkillEffectsFactory(IInGameLogger logger)
	{
		_logger = logger;
	}

	ISkillEffect[] ISkillEffectsFactory.GetSkillEffects(SkillStatsConfig skillConfig)
	{
		var effectConfigs = skillConfig.Effects;
		var effects = new ISkillEffect[effectConfigs.Length];
		
		for (var i = 0; i < effectConfigs.Length; i++)
		{
			var effectConfig = effectConfigs[i];
			var effect = GetSkillEffect(effectConfig);
			effects[i] = effect;
		}

		if (effects.Length == 0)
		{
			_logger.LogWarning("Skill has no effects");
		}

		return effects;
	}

	private ISkillEffect GetSkillEffect(IEffectConfig effectConfig)
	{
		var effectId = effectConfig.EffectId;
		if (effectConfig is PercentBuffAttackEffectConfig percentBuffAttackEffectConfig)
		{
			return new PercentBuffAttackSkillEffect(effectId, percentBuffAttackEffectConfig.AttackBuffPercent,
				percentBuffAttackEffectConfig.AttackBuffPercent);
		}

		if (effectConfig is InstantDamageEffectConfig instantDamageEffectConfig)
		{
			return new DamageSkillEffect(effectId, instantDamageEffectConfig.DamageAmount);
		}

		if (effectConfig is OverTimeDamageEffectConfig overTimeDamageEffectConfig)
		{
			return new DamageOverTimeSkillEffect(effectId, _logger, overTimeDamageEffectConfig.DurationPerMilliseconds,
				overTimeDamageEffectConfig.TotalDamage, overTimeDamageEffectConfig.IntervalPerMilliseconds);
		}

		if (effectConfig is InstantHealEffectConfig instantHealEffectConfig)
		{
			return new HealSkillEffect(effectId, instantHealEffectConfig.HealAmount);
		}

		if (effectConfig is OverTimeHealEffectConfig overTimeHealEffectConfig)
		{
			return new HealOverTimerSkillEffect(effectId, _logger, overTimeHealEffectConfig.DurationPerMilliseconds,
				overTimeHealEffectConfig.TotalHeal, overTimeHealEffectConfig.IntervalPerMilliseconds);
		}
		
		_logger.LogError($"Effect with id {effectId} does not have implementation");
		return null;
	}
}
}
using Code.GameConfig.ScriptableObjectParser.ConfigData.Effect;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Effects;
using Code.Skills.CharacterSkill.Effects.Buff;
using Code.Skills.CharacterSkill.Effects.Damage;
using Code.Skills.CharacterSkill.Effects.Heal;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Factory.Effects
{
/// <summary>
/// Concrete implementation of <see cref="ISkillEffectsFactory"/> that creates various skill effects,
/// such as damage, heal, and buffs, based on the provided configuration.
/// </summary>
internal class SkillEffectsFactory : ISkillEffectsFactory
{
	private readonly IInGameLogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="SkillEffectsFactory"/> class.
	/// </summary>
	/// <param name="logger">Logger to record any warnings or errors regarding skill effects.</param>
	internal SkillEffectsFactory(IInGameLogger logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Iterates over all configured effects in <paramref name="skillConfig"/> and attempts
	/// to create an appropriate <see cref="ISkillEffect"/> for each one. If the skill has no
	/// effects, a warning is logged.
	/// </remarks>
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
			return new DamageOverTimeSkillEffect(effectId, _logger, overTimeDamageEffectConfig.EffectDuration,
				overTimeDamageEffectConfig.TotalDamage, overTimeDamageEffectConfig.TimeBetweenDamage);
		}

		if (effectConfig is InstantHealEffectConfig instantHealEffectConfig)
		{
			return new HealSkillEffect(effectId, instantHealEffectConfig.HealAmount);
		}

		if (effectConfig is OverTimeHealEffectConfig overTimeHealEffectConfig)
		{
			return new HealOverTimerSkillEffect(effectId, _logger, overTimeHealEffectConfig.EffectDuration,
				overTimeHealEffectConfig.TotalHeal, overTimeHealEffectConfig.TimeBetweenHeal);
		}
		
		_logger.LogError($"Effect with id {effectId} does not have implementation");
		return null;
	}
}
}
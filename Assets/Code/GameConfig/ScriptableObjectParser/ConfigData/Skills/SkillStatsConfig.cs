using Code.GameConfig.ScriptableObjectParser.ConfigData.Effect;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public struct SkillStatsConfig
{
	public int Level { get; }
	public int CooldownPerMilliseconds { get; }
	public int ChargeTimePerMilliseconds { get; }
	public IEffectConfig[] Effects { get; }

	public SkillStatsConfig(int level, int cooldownPerMilliseconds, int chargeTimePerMilliseconds, IEffectConfig[] effects)
	{
		Level = level;
		CooldownPerMilliseconds = cooldownPerMilliseconds;
		ChargeTimePerMilliseconds = chargeTimePerMilliseconds;
		Effects = effects;
	}
}
}
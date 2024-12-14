namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public struct SkillImpactConfig
{
	public int Level { get; }
	public int Impact { get; }
	public int CooldownPerMilliseconds { get; }
	public int ChargeTimePerMilliseconds { get; }
	
	public SkillImpactConfig(int level, int impact, int cooldownPerMilliseconds, int chargeTimePerMilliseconds)
	{
		Level = level;
		Impact = impact;
		CooldownPerMilliseconds = cooldownPerMilliseconds;
		ChargeTimePerMilliseconds = chargeTimePerMilliseconds;
	}
}
}
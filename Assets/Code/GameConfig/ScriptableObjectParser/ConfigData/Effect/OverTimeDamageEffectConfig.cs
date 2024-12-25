namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct OverTimeDamageEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int TotalDamage { get;}
	public int IntervalPerMilliseconds { get; }
	public int DurationPerMilliseconds { get; }

	public OverTimeDamageEffectConfig(
		string effectId,
		int totalDamage,
		int intervalPerMilliseconds,
		int durationPerMilliseconds)
	{
		EffectId = effectId;
		TotalDamage = totalDamage;
		IntervalPerMilliseconds = intervalPerMilliseconds;
		DurationPerMilliseconds = durationPerMilliseconds;
	}
}
}
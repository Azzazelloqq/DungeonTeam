namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct OverTimeHealEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int TotalHeal { get; }
	public int IntervalPerMilliseconds { get; }
	public int DurationPerMilliseconds { get; }

	public OverTimeHealEffectConfig(string effectId, int totalHeal, int intervalPerMilliseconds, int durationPerMilliseconds)
	{
		EffectId = effectId;
		TotalHeal = totalHeal;
		IntervalPerMilliseconds = intervalPerMilliseconds;
		DurationPerMilliseconds = durationPerMilliseconds;
	}
}
}
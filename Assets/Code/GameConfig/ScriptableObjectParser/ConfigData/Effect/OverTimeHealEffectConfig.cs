namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct OverTimeHealEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int TotalHeal { get; }
	public int TimeBetweenHeal { get; }
	public int EffectDuration { get; }

	public OverTimeHealEffectConfig(string effectId, int totalHeal, int timeBetweenHeal, int effectDuration)
	{
		EffectId = effectId;
		TotalHeal = totalHeal;
		TimeBetweenHeal = timeBetweenHeal;
		EffectDuration = effectDuration;
	}
}
}
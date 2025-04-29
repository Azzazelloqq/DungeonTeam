namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct OverTimeDamageEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int TotalDamage { get; }
	public int TimeBetweenDamage { get; }
	public int EffectDuration { get; }

	public OverTimeDamageEffectConfig(
		string effectId,
		int totalDamage,
		int timeBetweenDamage,
		int effectDuration)
	{
		EffectId = effectId;
		TotalDamage = totalDamage;
		TimeBetweenDamage = timeBetweenDamage;
		EffectDuration = effectDuration;
	}
}
}
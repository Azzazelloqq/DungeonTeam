namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct InstantHealEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int HealAmount { get; }

	public InstantHealEffectConfig(string effectId, int healAmount)
	{
		EffectId = effectId;
		HealAmount = healAmount;
	}
}
}
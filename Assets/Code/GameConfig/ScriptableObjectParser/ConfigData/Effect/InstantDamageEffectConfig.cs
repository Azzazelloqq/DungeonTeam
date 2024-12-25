namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct InstantDamageEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int DamageAmount { get; }

	public InstantDamageEffectConfig(string effectId, int damageAmount)
	{
		EffectId = effectId;
		DamageAmount = damageAmount;
	}
}
}
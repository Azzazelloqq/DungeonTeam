namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Effect
{
public struct PercentBuffAttackEffectConfig : IEffectConfig
{
	public string EffectId { get; }
	public int AttackBuffPercent { get; }
	public int BuffDuration { get; }

	public PercentBuffAttackEffectConfig(string effectId, int attackBuffPercent, int buffDuration)
	{
		EffectId = effectId;
		AttackBuffPercent = attackBuffPercent;
		BuffDuration = buffDuration;
	}
}
}
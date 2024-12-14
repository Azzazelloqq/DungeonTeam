namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public readonly struct SkillConfig
{
	public string Id { get; }
	public SkillImpactConfig[] Impact { get; }
	public SkillType Type { get; }

	public SkillConfig(string id, SkillImpactConfig[] impact, SkillType type)
	{
		Id = id;
		Impact = impact;
		Type = type;
	}
}
}
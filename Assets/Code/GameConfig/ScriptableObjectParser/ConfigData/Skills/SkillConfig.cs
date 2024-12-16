using System.Collections.Generic;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public readonly struct SkillConfig
{
	public string Id { get; }
	public Dictionary<int, SkillStatsConfig> ImpactByLevel { get; }
	public SkillType Type { get; }

	public SkillConfig(string id, Dictionary<int, SkillStatsConfig> impactByLevel, SkillType type)
	{
		Id = id;
		ImpactByLevel = impactByLevel;
		Type = type;
	}
}
}
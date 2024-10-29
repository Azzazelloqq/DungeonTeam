using Code.Config;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Skills;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public struct SkillConfig : IConfigData
{
	public string Id { get; }
	public SkillImpact[] Impact { get; }

	public SkillConfig(string id, SkillImpact[] impact)
	{
		Id = id;
		Impact = impact;
	}
}
}
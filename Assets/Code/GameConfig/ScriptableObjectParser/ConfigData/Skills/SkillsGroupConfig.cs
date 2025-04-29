using System.Collections.Generic;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public class SkillsGroupConfig
{
	public SkillType SkillType { get; }

	public IReadOnlyDictionary<string, SkillConfig> Skills => _skills;

	private readonly Dictionary<string, SkillConfig> _skills;

	public SkillsGroupConfig(SkillType skillType, Dictionary<string, SkillConfig> skills)
	{
		SkillType = skillType;
		_skills = skills;
	}
}
}
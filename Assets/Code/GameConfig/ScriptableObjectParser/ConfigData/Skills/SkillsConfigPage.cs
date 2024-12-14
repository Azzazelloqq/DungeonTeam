using System.Collections.Generic;
using Code.Config;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Skills
{
public readonly struct SkillsConfigPage : IConfigPage
{
	public IReadOnlyDictionary<SkillType, SkillsGroupConfig> SkillsGroups => _skillsGroups;
	
	private readonly Dictionary<SkillType, SkillsGroupConfig> _skillsGroups;

	public SkillsConfigPage(Dictionary<SkillType, SkillsGroupConfig> skillsGroups)
	{
		_skillsGroups = skillsGroups;
	}
}
}
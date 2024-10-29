using System;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
public struct SkillImpact
{
	public int Level { get; }
	public int Impact { get; }
	
	public SkillImpact(int level, int impact)
	{
		Level = level;
		Impact = impact;
	}
}
}
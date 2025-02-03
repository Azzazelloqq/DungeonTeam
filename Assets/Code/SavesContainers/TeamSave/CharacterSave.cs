using System;
using System.Collections.Generic;

namespace Code.SavesContainers.TeamSave
{
[Serializable]
public class CharacterSave
{
	public string Id { get; }
	public int CurrentHealth { get; internal set; }
	public int CurrentLevel { get; internal set; }
	public IReadOnlyList<CharacterSkillSave> ReadOnlySkills => Skills;	
	internal List<CharacterSkillSave> Skills { get; }
	
	public CharacterSave(string id, int currentLevel, int currentHealth, CharacterSkillSave[] skills)
	{
		Id = id;
		CurrentLevel = currentLevel;
		CurrentHealth = currentHealth;
		Skills = new List<CharacterSkillSave>(skills);
	}
}
}
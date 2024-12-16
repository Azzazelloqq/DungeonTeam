using System;

namespace Code.SavesContainers.TeamSave
{
public readonly struct CharacterSave : IEquatable<CharacterSave>
{
	public string Id { get; }
	public int CurrentHealth { get; }
	public int CurrentLevel { get; }
	public CharacterSkillSave[] Skills { get; }
	
	public CharacterSave(string id, int currentLevel, int currentHealth, CharacterSkillSave[] skills)
	{
		Id = id;
		CurrentLevel = currentLevel;
		CurrentHealth = currentHealth;
		Skills = skills;
	}

	public bool Equals(CharacterSave other)
	{
		return Id == other.Id;
	}

	public override bool Equals(object obj)
	{
		return obj is CharacterSave other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, CurrentHealth, CurrentLevel);
	}
}
}
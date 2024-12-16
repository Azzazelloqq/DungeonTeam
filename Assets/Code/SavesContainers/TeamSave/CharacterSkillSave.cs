namespace Code.SavesContainers.TeamSave
{
public readonly struct CharacterSkillSave
{
	public string Id { get; }
	public int SkillLevel { get; }

	public CharacterSkillSave(string id, int skillLevel = 0)
	{
		Id = id;
		SkillLevel = skillLevel;
	}
}
}
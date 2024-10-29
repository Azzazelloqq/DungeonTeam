using Code.Config;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public struct CharacterConfig : IConfigData
{
	public string Id { get; }
	public string[] Skills { get; }
	
	public CharacterConfig(string id, string[] skills)
	{
		Id = id;
		Skills = skills;
	}
}
}
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterConfig
{
	public string Id { get; }
	public string[] Skills { get; }
	public CharacterClass CharacterClass { get; }
	public CharacterAttackConfig AttackConfig { get; }
	public CharacterHealthByLevelConfig[] CharacterHealthByLevelConfig { get; }

	public CharacterConfig(
		string id,
		string[] skills,
		CharacterClass characterClass,
		CharacterAttackConfig attackConfig,
		CharacterHealthByLevelConfig[] characterHealthByLevelConfig)
	{
		Id = id;
		Skills = skills;
		CharacterClass = characterClass;
		AttackConfig = attackConfig;
		CharacterHealthByLevelConfig = characterHealthByLevelConfig;
	}
}
}
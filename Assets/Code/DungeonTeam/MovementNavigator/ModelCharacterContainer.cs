using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;

namespace Code.DungeonTeam.MovementNavigator
{
public struct ModelCharacterContainer
{
	public string Id { get; }
	public CharacterClass CharacterClass { get; }

	public ModelCharacterContainer(string id, CharacterClass characterClass)
	{
		Id = id;
		CharacterClass = characterClass;
	}
}
}
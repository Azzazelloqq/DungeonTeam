using Code.Config;
using Code.DungeonTeam.TeamCharacter;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace
{
public struct CharacterTeamPlaceConfig : IConfigData
{
	public int PlaceNumber { get; }
	public CharacterClass[] ClassesForPlace { get; }
	
	public CharacterTeamPlaceConfig(CharacterClass[] classesForPlace, int placeNumber)
	{
		ClassesForPlace = classesForPlace;
		PlaceNumber = placeNumber;
	}
}
}
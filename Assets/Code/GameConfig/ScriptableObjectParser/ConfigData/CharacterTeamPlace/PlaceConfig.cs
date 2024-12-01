using Code.DungeonTeam.TeamCharacter;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace
{
	public struct PlaceConfig
	{
		public int PlaceNumber { get; }
		public CharacterClass PreferredClass { get; }

		public PlaceConfig(int placeNumber, CharacterClass preferredClass)
		{
			PlaceNumber = placeNumber;
			PreferredClass = preferredClass;
		}
	}
}
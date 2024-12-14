namespace Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace
{
public readonly struct PlaceConfig
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
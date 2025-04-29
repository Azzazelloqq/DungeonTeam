using Code.Config;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace
{
public readonly struct CharacterTeamMoveConfigPage : IConfigPage
{
	public PlaceConfig[] PlaceConfigs { get; }
	public float TeamSpeed { get; }

	public CharacterTeamMoveConfigPage(PlaceConfig[] placeConfigs, float teamSpeed)
	{
		PlaceConfigs = placeConfigs;
		TeamSpeed = teamSpeed;
	}
}
}
using Code.Config;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace
{
public readonly struct CharacterTeamMoveConfig : IConfigData
{
	public PlaceConfig[] PlaceConfigs { get; }
	public float TeamSpeed { get; }
	public CharacterTeamMoveConfig(PlaceConfig[] placeConfigs, float teamSpeed)
	{
		PlaceConfigs = placeConfigs;
		TeamSpeed = teamSpeed;
	}
}
}
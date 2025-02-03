namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterHealthByLevelConfig
{
	public int Level { get; }
	public int MaxHealth { get; }

	public CharacterHealthByLevelConfig(int level, int maxHealth)
	{
		Level = level;
		MaxHealth = maxHealth;
	}
}
}
namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterAttack
{
	public int Level { get; }
	public int ReloadAttackTime { get; }
	public int AttackDamage { get; }
	public int AttackCastTime { get; }

	public CharacterAttack(int level, int reloadAttackTime, int attackDamage, int attackCastTime)
	{
		Level = level;
		ReloadAttackTime = reloadAttackTime;
		AttackDamage = attackDamage;
		AttackCastTime = attackCastTime;
	}
}
}
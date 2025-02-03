using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;
using Code.Utils.FloatUtils;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterAttack
{
	public int Level { get; }
	public int ReloadAttackPerMilliseconds { get; }
	public int AttackDamage { get;}

	public CharacterAttack(int level, int reloadAttackPerMilliseconds, int attackDamage)
	{
		Level = level;
		ReloadAttackPerMilliseconds = reloadAttackPerMilliseconds;
		AttackDamage = attackDamage;
	}

	public CharacterAttack(CharacterAttackByLevelRemote level)
	{
		Level = level.Level;
		ReloadAttackPerMilliseconds = level.ReloadAttackPerSeconds.ToMilliseconds();
		AttackDamage = level.AttackDamage;
	}
}
}
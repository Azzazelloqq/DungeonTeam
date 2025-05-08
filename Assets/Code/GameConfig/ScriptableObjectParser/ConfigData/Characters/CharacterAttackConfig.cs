using System.Collections.Generic;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;
using Code.Utils.ValuesUtils;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterAttackConfig
{
	public float ViewDistance { get; }
	public float ViewAngel { get; }
	public float AttackSkillDistance { get; }
	public float AttackDistance { get; }
	public float InvokeAttackNormalizedTime { get; }
	public Dictionary<int, CharacterAttack> AttackByLevels { get; }

	internal CharacterAttackConfig(
		float viewDistance,
		float viewAngel,
		float attackSkillDistance,
		float attackDistance,
		float invokeAttackNormalizedTime,
		CharacterAttackByLevelRemote[] attackByLevels)
	{
		ViewDistance = viewDistance;
		ViewAngel = viewAngel;
		AttackSkillDistance = attackSkillDistance;
		AttackDistance = attackDistance;
		InvokeAttackNormalizedTime = invokeAttackNormalizedTime;

		AttackByLevels = new Dictionary<int, CharacterAttack>(attackByLevels.Length);

		foreach (var attackRemote in attackByLevels)
		{
			var level = attackRemote.Level;
			var reloadAttack = attackRemote.ReloadAttackPerSeconds.ToMilliseconds();
			var attackDamage = attackRemote.AttackDamage;
			var castTime = attackRemote.CastPerSeconds.ToMilliseconds();

			AttackByLevels[level] = new CharacterAttack(level, reloadAttack, attackDamage, castTime);
		}
	}

	internal CharacterAttackConfig(CharacterAttackRemote remoteConfig)
	{
		ViewDistance = remoteConfig.ViewDistance;
		ViewAngel = remoteConfig.ViewAngel;
		AttackSkillDistance = remoteConfig.AttackSkillDistance;
		AttackDistance = remoteConfig.AttackDistance;
		InvokeAttackNormalizedTime = remoteConfig.InvokeAttackNormalizedTime;

		var attackByLevelsRemote = remoteConfig.AttackByLevels;
		AttackByLevels = new Dictionary<int, CharacterAttack>(attackByLevelsRemote.Length);

		foreach (var attackRemote in attackByLevelsRemote)
		{
			var level = attackRemote.Level;
			var reloadAttack = attackRemote.ReloadAttackPerSeconds.ToMilliseconds();
			var attackDamage = attackRemote.AttackDamage;
			var castTime = attackRemote.CastPerSeconds.ToMilliseconds();

			AttackByLevels[level] = new CharacterAttack(level, reloadAttack, attackDamage, castTime);
		}
	}
}
}
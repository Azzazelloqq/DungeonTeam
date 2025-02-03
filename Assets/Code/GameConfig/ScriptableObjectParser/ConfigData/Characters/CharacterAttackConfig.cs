using System.Collections.Generic;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharacterAttackConfig
{
	public int AttackLayer { get; }
	public float ViewDistance { get; }
	public float ViewAngel { get; }
	public float AttackSkillDistance { get; }
	public float AttackDistance { get; }
	public Dictionary<int, CharacterAttack> AttackByLevels { get; }
	
	internal CharacterAttackConfig(
		int attackLayer,
		float viewDistance,
		float viewAngel,
		float attackSkillDistance,
		float attackDistance,
		CharacterAttackByLevelRemote[] attackByLevels)
	{
		AttackLayer = attackLayer;
		ViewDistance = viewDistance;
		ViewAngel = viewAngel;
		AttackSkillDistance = attackSkillDistance;
		AttackDistance = attackDistance;
		
		AttackByLevels = new Dictionary<int, CharacterAttack>(attackByLevels.Length);
		
		foreach (var attackRemote in attackByLevels)
		{
			var level = attackRemote.Level;
			
			AttackByLevels[level] = new CharacterAttack(attackRemote);
		}
	}

	internal CharacterAttackConfig(CharacterAttackRemote remoteConfig)
	{
		AttackLayer = remoteConfig.AttackLayer;
		ViewDistance = remoteConfig.ViewDistance;
		ViewAngel = remoteConfig.ViewAngel;
		AttackSkillDistance = remoteConfig.AttackSkillDistance;
		AttackDistance = remoteConfig.AttackDistance;
		
		var attackByLevelsRemote = remoteConfig.AttackByLevels;
		AttackByLevels = new Dictionary<int, CharacterAttack>(attackByLevelsRemote.Length);
		
		foreach (var attackRemote in attackByLevelsRemote)
		{
			var level = attackRemote.Level;
			
			AttackByLevels[level] = new CharacterAttack(attackRemote);
		}
	}
}
}
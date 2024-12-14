using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public struct CharacterAttackConfig
{
	public int AttackLayer { get; }
	public float ViewDistance { get; }
	public float ViewAngel { get; }
	public float AttackSkillDistance { get; }
	public float AttackDistance { get; }
	
	public CharacterAttackConfig(int attackLayer, float viewDistance, float viewAngel, float attackSkillDistance, float attackDistance)
	{
		AttackLayer = attackLayer;
		ViewDistance = viewDistance;
		ViewAngel = viewAngel;
		AttackSkillDistance = attackSkillDistance;
		AttackDistance = attackDistance;
	}

	public CharacterAttackConfig(CharacterAttackRemote remoteConfig)
	{
		AttackLayer = remoteConfig.AttackLayer;
		ViewDistance = remoteConfig.ViewDistance;
		ViewAngel = remoteConfig.ViewAngel;
		AttackSkillDistance = remoteConfig.AttackSkillDistance;
		AttackDistance = remoteConfig.AttackDistance;
	}
}
}
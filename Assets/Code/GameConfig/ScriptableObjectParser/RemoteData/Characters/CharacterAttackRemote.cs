using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
internal struct CharacterAttackRemote
{
	[field: SerializeField]
	public int AttackLayer { get; private set; }

	[field: SerializeField]
	public float ViewDistance { get; private set; }

	[field: SerializeField]
	public float ViewAngel { get; private set; }

	[field: SerializeField]
	public float AttackSkillDistance { get; private set; }

	[field: SerializeField]
	public float AttackDistance { get; private set; }

	[field: SerializeField]
	public CharacterAttackByLevelRemote[] AttackByLevels { get; private set; }
}
}
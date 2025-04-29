using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
internal struct CharacterAttackRemote
{
	[field: SerializeField]
	public float ViewDistance { get; private set; }

	[field: SerializeField]
	public float ViewAngel { get; private set; }

	[field: SerializeField]
	public float AttackSkillDistance { get; private set; }

	[field: SerializeField]
	public float AttackDistance { get; private set; }

	[Tooltip("Normalized time (0-1) when the attack effect/damage should be applied during the attack animation")]
	[Range(0, 1)]
	[SerializeField]
	private float _invokeAttackNormalizedTime;

	[field: SerializeField]
	public CharacterAttackByLevelRemote[] AttackByLevels { get; private set; }

	public float InvokeAttackNormalizedTime => _invokeAttackNormalizedTime;
}
}
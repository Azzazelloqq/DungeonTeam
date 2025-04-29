using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterAttackByLevelRemote
{
	[field: SerializeField]
	public int Level { get; private set; }

	[field: SerializeField]
	public float ReloadAttackPerSeconds { get; private set; }

	[field: SerializeField]
	public int AttackDamage { get; private set; }

	[SerializeField]
	private float _castPerSeconds;

	public float CastPerSeconds => _castPerSeconds;
}
}
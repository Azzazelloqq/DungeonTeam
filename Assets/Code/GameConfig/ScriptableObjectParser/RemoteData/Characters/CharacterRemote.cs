using System;
using Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterRemote
{
	[field: SerializeField]
	internal string Id { get; private set; }

	[field: SerializeField]
	internal string[] Skills { get; private set; }

	[field: SerializeField]
	internal CharacterClassRemote CharacterClass { get; private set; }

	[field: SerializeField]
	internal CharacterAttackRemote AttackInfo { get; private set; }

	[field: SerializeField]
	internal CharacterHealthByLevelRemote[] HealthByLevel { get; private set; }
}
}
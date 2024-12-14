using System;
using Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterRemote
{
	[field: SerializeField]
	public string Id { get; private set; }
	
	[field: SerializeField]
	public string[] Skills { get; private set; }
	
	[field: SerializeField]
	public CharacterClassRemote CharacterClass { get; private set; }
	
	[field: SerializeField]
	public CharacterAttackRemote AttackInfo { get; private set; }
	
	[field: SerializeField]
	public CharacterHealthByLevelRemote[] HealthByLevel { get; private set; }
}
}
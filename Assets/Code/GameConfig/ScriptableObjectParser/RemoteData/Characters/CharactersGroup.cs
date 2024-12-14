using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharactersGroup
{
	[field: SerializeField]
	public string CharactersGroupId { get; private set; }
	
	[field: SerializeField]
	public CharacterRemote[] Characters { get; private set; } 
}
}
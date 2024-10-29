using System;
using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterRemote : IRemoteData
{
	[field: SerializeField]
	public string Id { get; private set; }
	
	[field: SerializeField]
	public string[] Skills { get; private set; }
}
}
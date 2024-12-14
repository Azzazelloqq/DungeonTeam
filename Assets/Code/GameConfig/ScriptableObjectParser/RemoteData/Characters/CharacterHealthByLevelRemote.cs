using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterHealthByLevelRemote
{
	[field: SerializeField]
	public int Level { get; private set; }

	[field: SerializeField]
	public int Health { get; private set; }
}
}
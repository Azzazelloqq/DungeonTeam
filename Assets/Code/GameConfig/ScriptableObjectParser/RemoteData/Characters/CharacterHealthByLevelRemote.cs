using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
public struct CharacterHealthByLevelRemote
{
	[field: SerializeField]
	internal int Level { get; private set; }

	[field: SerializeField]
	internal int Health { get; private set; }
}
}
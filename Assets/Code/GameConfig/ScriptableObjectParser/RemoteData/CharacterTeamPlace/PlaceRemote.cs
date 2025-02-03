using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
[Serializable]
internal struct PlaceRemote
{
	[field: SerializeField]
	internal int PlaceNumber { get; private set; }

	[field: SerializeField]
	internal CharacterClassRemote PreferredClass { get; private set; }
}
}
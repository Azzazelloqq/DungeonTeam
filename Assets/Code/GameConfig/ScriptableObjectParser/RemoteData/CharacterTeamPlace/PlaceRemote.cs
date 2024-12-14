using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
[Serializable]
public struct PlaceRemote
{
	[field: SerializeField]
	public int PlaceNumber { get; private set; }

	[field: SerializeField]
	public CharacterClassRemote PreferredClass { get; private set; }
}
}
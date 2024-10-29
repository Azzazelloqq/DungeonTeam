using System;
using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
[Serializable]
public struct CharacterTeamPlaceRemote : IRemoteData
{
	[field: SerializeField]
	public int PlaceNumber { get; private set; }

	[field: SerializeField]
	public CharacterClassType[] ClassesForPlace { get; private set; }
}
}
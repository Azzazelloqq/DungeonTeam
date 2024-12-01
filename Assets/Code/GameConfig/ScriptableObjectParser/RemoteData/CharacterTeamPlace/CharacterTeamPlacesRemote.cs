using System;
using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
	[Serializable]
	public struct CharacterTeamPlacesRemote : IRemoteData
	{
		[field: SerializeField]
		public PlaceRemote[] Places { get; private set; }
		
		[field: SerializeField]
		public float TeamSpeed { get; private set; }
	}
}
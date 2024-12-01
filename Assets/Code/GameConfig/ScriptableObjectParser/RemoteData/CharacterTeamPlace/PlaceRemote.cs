using System;
using Code.DungeonTeam.TeamCharacter;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
	[Serializable]
	public struct PlaceRemote
	{
		[field: SerializeField]
		public int PlaceNumber { get; private set; }
		
		[field: SerializeField]
		public CharacterClassConfigType PreferredClass { get; private set; }
	}
}
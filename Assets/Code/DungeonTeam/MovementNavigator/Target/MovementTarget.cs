using System;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator.Target
{
	[Serializable]
	public struct MovementTarget
	{
		[field: SerializeField]
		public int PlaceNum { get; private set; }
		
		[field: SerializeField]
		public Transform Place { get; private set; }
	}
}
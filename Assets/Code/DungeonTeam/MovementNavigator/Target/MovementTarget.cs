using System;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator.Target {
	[Serializable]
	public struct MovementTarget {
		[field: SerializeField]
		public int CharactersCount { get; private set; }
		
		[field: SerializeField]
		public TargetPlace[] Targets { get; private set; }
	}
}
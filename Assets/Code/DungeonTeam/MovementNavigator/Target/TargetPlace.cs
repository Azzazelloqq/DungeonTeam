using System;
using Code.DungeonTeam.TeamCharacter;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator.Target
{
[Serializable]
public struct TargetPlace
{
	[field: SerializeField]
	public CharacterClass CharacterClass { get; private set; }
	
	[field: SerializeField]
	public Transform Place { get; private set; }
}
}
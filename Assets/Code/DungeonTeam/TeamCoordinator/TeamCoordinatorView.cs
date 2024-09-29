using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.DungeonTeam.TeamCoordinator.Base;
using UnityEngine;

namespace Code.DungeonTeam.TeamCoordinator
{
public class TeamCoordinatorView : TeamCoordinatorViewBase
{
	[field: SerializeField]
	public override MovementNavigatorView MovementNavigatorView { get; protected set; }
	
	[field: SerializeField]
	public override TeamCharacterViewBase CharacterViewPrefab { get; protected set; }
}
}
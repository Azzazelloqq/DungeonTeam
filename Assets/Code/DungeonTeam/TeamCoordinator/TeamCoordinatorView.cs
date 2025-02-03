using Code.DungeonTeam.TeamCoordinator.Base;
using UnityEngine;

namespace Code.DungeonTeam.TeamCoordinator
{
public class TeamCoordinatorView : TeamCoordinatorViewBase
{
	[field: SerializeField]
	public override Transform SkillsParent { get; protected set; }

	[field: SerializeField]
	public override Transform MovementNavigatorParent { get; protected set; }
	
	[field: SerializeField]
	public override Transform CharactersParent { get; protected set; }
}
}
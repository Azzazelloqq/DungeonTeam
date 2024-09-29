using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.MovementNavigator.Target;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorView : MovementNavigatorViewBase
{
	[field: SerializeField]
	public override MovementTarget[] MovementTargets { get; protected set; }
	
}
}
using Code.DungeonTeam.MovementNavigator.Target;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator.Base
{
public abstract class MovementNavigatorViewBase : ViewMonoBehaviour<MovementNavigatorPresenterBase>
{
	public abstract MovementTarget[] MovementTargets { get; protected set; }
	public abstract Vector3 TeamParentPosition { get; }
	public abstract void MoveTeamToPosition(Vector3 teamPosition);
}
}
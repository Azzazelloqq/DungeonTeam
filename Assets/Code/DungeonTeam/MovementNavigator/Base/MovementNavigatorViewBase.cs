using Code.DungeonTeam.MovementNavigator.Target;
using MVP;

namespace Code.DungeonTeam.MovementNavigator.Base
{
public abstract class MovementNavigatorViewBase : ViewMonoBehaviour<MovementNavigatorPresenterBase>
{
	public abstract MovementTarget[] MovementTargets { get; protected set; }
}
}
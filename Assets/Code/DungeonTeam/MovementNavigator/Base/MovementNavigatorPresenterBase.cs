using MVP;

namespace Code.DungeonTeam.MovementNavigator.Base
{
public abstract class MovementNavigatorPresenterBase : Presenter<MovementNavigatorViewBase, MovementNavigatorModelBase>
{
	protected MovementNavigatorPresenterBase(MovementNavigatorViewBase view, MovementNavigatorModelBase model) : base(view, model)
	{
	}
}
}
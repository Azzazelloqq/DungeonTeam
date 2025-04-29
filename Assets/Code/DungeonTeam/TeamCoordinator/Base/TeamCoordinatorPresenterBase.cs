using MVP;

namespace Code.DungeonTeam.TeamCoordinator.Base
{
public abstract class TeamCoordinatorPresenterBase : Presenter<TeamCoordinatorViewBase, TeamCoordinatorModelBase>
{
	protected TeamCoordinatorPresenterBase(TeamCoordinatorViewBase view, TeamCoordinatorModelBase model) : base(view, model)
	{
	}
}
}
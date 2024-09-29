using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.TeamCoordinator.Base;
using ResourceLoader;

namespace Code.DungeonTeam.TeamCoordinator
{
public class TeamCoordinatorPresenter : TeamCoordinatorPresenterBase
{
	private readonly IResourceLoader _resourceLoader;
	private MovementNavigatorPresenter _movementNavigator;

	public TeamCoordinatorPresenter(TeamCoordinatorViewBase view, TeamCoordinatorModelBase model,
		IResourceLoader resourceLoader) : base(view, model)
	{
		_resourceLoader = resourceLoader;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
	}
}
}
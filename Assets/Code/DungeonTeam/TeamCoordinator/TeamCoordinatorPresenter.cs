using Code.Config;
using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.TeamCoordinator.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader;
using TickHandler;

namespace Code.DungeonTeam.TeamCoordinator
{
public class TeamCoordinatorPresenter : TeamCoordinatorPresenterBase
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IConfig _config;
	private readonly MovementNavigatorPresenter _movementNavigator;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;
	private readonly ILocalSaveSystem _saveSystem;

	public TeamCoordinatorPresenter(
		TeamCoordinatorViewBase view,
		TeamCoordinatorModelBase model,
		IResourceLoader resourceLoader,
		IConfig config,
		IInGameLogger logger,
		ITickHandler tickHandler,
		ILocalSaveSystem saveSystem) : base(view,
		model) {
		_resourceLoader = resourceLoader;
		_config = config;
		_logger = logger;
		_tickHandler = tickHandler;
		_saveSystem = saveSystem;

		var moveConfig = _config.GetData<CharacterTeamMoveConfig>();
		var movementNavigatorModelBase = new MovementNavigatorModel(moveConfig, _logger);
		//_movementNavigator = new MovementNavigatorPresenter(view.MovementNavigatorView, movementNavigatorModelBase, _logger, _tickHandler);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();

		
	}
	
	// private TeamCharacterPresenterBase[] initTeamCharacters()
	// {
	// 	_saveSystem.Load<>()
	// }
}
}
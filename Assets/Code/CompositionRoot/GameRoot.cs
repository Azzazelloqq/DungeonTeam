using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DetectionService;
using Code.DungeonTeam.MoveController.Base;
using Code.DungeonTeam.MoveController.VirtualJoystick;
using Code.DungeonTeam.TeamCoordinator;
using Code.DungeonTeam.TeamCoordinator.Base;
using Code.EnemiesCore.Enemies.Base.BaseEnemy;
using Code.EnemiesCore.Enemies.TestTeamEnemy;
using Code.GameConfig.ScriptableObjectParser;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.MovementService;
using Code.SavesContainers.Factory;
using Code.SavesContainers.TeamSave;
using Code.UI.UIContext;
using Code.Utils.AsyncUtils;
using Disposable;
using InGameLogger;
using LightDI.Runtime;
using LocalSaveSystem;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code.CompositionRoot
{
[DefaultExecutionOrder(-1000)]
public class GameCompositionRoot : MonoBehaviourDisposable, ICompositionRoot
{
	private const string SavesFolderName = "Saves";
	private const float DetectionCellSize = 30;

	[SerializeField]
	private UnityDispatcherBehaviour _dispatcherBehaviour;

	[SerializeField]
	private ScriptableObjectConfig _scriptableObjectConfig;

	[SerializeField]
	private CanvasUIContext _canvasUIContext;

	[SerializeField]
	private Transform _teamCoordinatorParent;

	[SerializeField]
	private Transform _testEnemyParent;

	#if UNITY_EDITOR
	[SerializeField]
	private DetectionServiceGizmos _detectionServiceGizmos;
	#endif

	private readonly CancellationTokenSource _applicationQuitTokenSource;
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly IDetectionService _detectionService;
	private readonly IDiContainer _gameDiContainer;
	private ILocalSaveSystem _saveSystem;
	private ITickHandler _tickHandler;
	private IMovementService _movementService;
	private IConfig _config;
	private TeamCoordinatorPresenterBase _teamCoordinator;

	public GameCompositionRoot()
	{
		_gameDiContainer = DiContainerFactory.CreateContainer();
		
		_logger = new UnityInGameLogger();
		_gameDiContainer.RegisterAsSingleton(_logger);
		
		_resourceLoader = new AddressableResourceLoader();
		_gameDiContainer.RegisterAsSingleton(_resourceLoader);
		
		_detectionService = new DetectionService.DetectionService(DetectionCellSize);
		_gameDiContainer.RegisterAsSingleton(_detectionService);
		
		_applicationQuitTokenSource = new CancellationTokenSource();
	}

	private async void Start()
	{
		try
		{
			var cancellationToken = _applicationQuitTokenSource.Token;

			await EnterAsync(cancellationToken);
		}
		catch (Exception e)
		{
			if (e is not OperationCanceledException)
			{
				_logger.LogError(e);
			}
		}
	}

	public void Enter()
	{
		throw new NotImplementedException();
	}

	public async Task EnterAsync(CancellationToken token)
	{
		try
		{
			await InitializeRoot(token);
		}
		catch (TaskCanceledException taskCanceledException)
		{
		}
		catch (Exception e)
		{
			_logger.LogException(e);
		}
	}

	private void OnApplicationQuit()
	{
		_applicationQuitTokenSource.Cancel();
		DiContainerProvider.Dispose();
		
		Dispose();
	}
	
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		_applicationQuitTokenSource.CancelAndDispose();
		_dispatcherBehaviour.Dispose();
		_teamCoordinator.Dispose();
		_gameDiContainer.Dispose();
	}

	//just for testing
	private async Task InitializeRoot(CancellationToken cancellationToken)
	{
		_gameDiContainer.RegisterAsSingleton<IUIContext>(_canvasUIContext);
		
		_tickHandler = new UnityTickHandler(_dispatcherBehaviour);
		_gameDiContainer.RegisterAsSingleton(_tickHandler);
		
		_movementService = new GenericMovementService(_tickHandler, _logger);
		_gameDiContainer.RegisterAsSingleton(_movementService);
		
		var savePath = Path.Combine(Application.persistentDataPath, SavesFolderName);
		var saveFactory = new SaveFactory();
		_saveSystem = new UnityBinaryLocalSaveSystem(savePath);
		await _saveSystem.InitializeSavesAsync(saveFactory, cancellationToken);
		_gameDiContainer.RegisterAsSingleton(_saveSystem);

		_config = await InitializeConfigAsync(cancellationToken);
		_gameDiContainer.RegisterAsSingleton(_config);

		var playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		var charactersConfigPage = _config.GetConfigPage<CharactersConfigPage>();
		var skillsConfigPage = _config.GetConfigPage<SkillsConfigPage>();

		foreach (var characterConfig in charactersConfigPage.Characters.Values)
		{
			var characterConfigSkills = characterConfig.Skills;
			var skillSaves = new CharacterSkillSave[characterConfigSkills.Length];

			foreach (var unused in skillsConfigPage.SkillsGroups.Values)
			{
				for (var i = 0; i < characterConfigSkills.Length; i++)
				{
					var skillId = characterConfigSkills[i];
					skillSaves[i] = new CharacterSkillSave(skillId);
				}
			}

			var id = characterConfig.Id;
			var health = characterConfig.CharacterHealthByLevelConfig[0].MaxHealth;
			var characterSave = new CharacterSave(id, 0, health, skillSaves);
			playerTeamSave.AddCharacter(characterSave);
		}

		#if UNITY_EDITOR
		InitializeEditorServices();
		#endif

		var moveController = await InitializeMoveControllerAsync(cancellationToken);
		_gameDiContainer.RegisterAsSingleton(moveController);
		
		_saveSystem.Save();
		_teamCoordinator = await InitializeTeamCoordinatorAsync(cancellationToken);

		var testEnemy = await InitializeTestEnemy(cancellationToken);
	}

	private async Task<IConfig> InitializeConfigAsync(CancellationToken token)
	{
		var charactersRemotePage = _scriptableObjectConfig.CharactersRemotePage;
		var skillsRemotePage = _scriptableObjectConfig.SkillsRemotePage;
		var characterTeamPlacesRemotePage = _scriptableObjectConfig.CharacterTeamPlacesRemotePage;
		var detectRemotePage = _scriptableObjectConfig.DetectRemotePage;

		IRemotePage[] remoteDatas =
		{
			charactersRemotePage,
			skillsRemotePage,
			characterTeamPlacesRemotePage,
			detectRemotePage
		};

		var configParser = new ScriptableObjectConfigParser(remoteDatas, _logger);
		var config = new Config.Config(configParser);

		await config.InitializeAsync(token);

		return config;
	}

	private async Task<TeamCoordinatorPresenterBase> InitializeTeamCoordinatorAsync(CancellationToken token)
	{
		var view =
			await _resourceLoader.LoadAndCreateAsync<TeamCoordinatorViewBase, Transform>(
				ResourceIdsContainer.Test.TeamCoordinatorView, _teamCoordinatorParent, token);

		var teamCoordinatorModel = new TeamCoordinatorModel();
		var teamCoordinatorPresenter = TeamCoordinatorPresenterFactory.CreateTeamCoordinatorPresenter(
			view,
			teamCoordinatorModel);
		
		await teamCoordinatorPresenter.InitializeAsync(token);

		return teamCoordinatorPresenter;
	}

	private async Task<EnemyPresenterBase> InitializeTestEnemy(CancellationToken token)
	{
		var testEnemyResourceId = ResourceIdsContainer.Test.TestEnemy;
		var view = await _resourceLoader.LoadAndCreateAsync<EnemyViewBase, Transform>(testEnemyResourceId, _testEnemyParent,
			token);
		var model = new TestTakeDamageEnemyModel();

		var presenter = TestTakeDamageEnemyPresenterFactory.CreateTestTakeDamageEnemyPresenter(view, model);

		await presenter.InitializeAsync(token);

		return presenter;
	}
	
	private async Task<IMoveController> InitializeMoveControllerAsync(CancellationToken token)
	{
		var uiOverlayParent = _canvasUIContext.UIElementsOverlay;
		var moveControllerViewResourceId = ResourceIdsContainer.GameplayUI.VirtualJoystickView;
		var moveControllerView =
			await _resourceLoader.LoadAndCreateAsync<MoveControllerViewBase, Transform>(moveControllerViewResourceId, uiOverlayParent, token);

		var radius = moveControllerView.ControllerHandleRadius;
		var moveControllerModel = new VirtualJoystickModel(_logger, radius);
		
		var moveController = new VirtualJoystickPresenter(moveControllerView, moveControllerModel);
		
		await moveController.InitializeAsync(token);
		
		return moveController;
	}

	#if UNITY_EDITOR
	private void InitializeEditorServices()
	{
		_detectionServiceGizmos.Initialize(_detectionService);
	}
	#endif
}
}
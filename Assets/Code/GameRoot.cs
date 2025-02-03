using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DetectionService;
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
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader.AddressableResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code
{
public class GameRoot : MonoBehaviour
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
	
	private readonly CancellationTokenSource _turnOffGameCancellationTokenSource;
	private readonly AddressableResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private ILocalSaveSystem _saveSystem;
	private readonly IDetectionService _detectionService;
	private ITickHandler _tickHandler;
	private IMovementService _movementService;
	private Config.Config _config;
	private TeamCoordinatorPresenterBase _teamCoordinator;

	public GameRoot()
	{
		_logger = new UnityInGameLogger();
		_resourceLoader = new AddressableResourceLoader();
		_logger = new UnityInGameLogger();
		
		_detectionService = new DetectionService.DetectionService(DetectionCellSize);
		_turnOffGameCancellationTokenSource = new CancellationTokenSource();
	}
	
	private async void Start()
	{
		try
		{
			await InitializeRoot();
		}
		catch (Exception e)
		{
			if(e is not OperationCanceledException)
			{
				_logger.LogError(e);
			}
		}
	}

	private void OnDestroy() {
		Dispose();
	}

	private void Dispose()
	{
		_turnOffGameCancellationTokenSource.CancelAndDispose();
		_dispatcherBehaviour.Dispose();
		_teamCoordinator.Dispose();
	}

	private async Task InitializeRoot()
	{
		var cancellationToken = _turnOffGameCancellationTokenSource.Token;
		
		_tickHandler = new UnityTickHandler(_dispatcherBehaviour);
		_movementService = new GenericMovementService(_tickHandler, _logger);
		
		var savePath = Path.Combine(Application.persistentDataPath, SavesFolderName);
		var saveFactory = new SaveFactory();
		_saveSystem = new UnityBinaryLocalSaveSystem(savePath);
		await _saveSystem.InitializeSavesAsync(saveFactory, cancellationToken);

		await InitializeConfigAsync(cancellationToken);

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
			var characterSave = new CharacterSave(id, 1, health, skillSaves);
			playerTeamSave.AddCharacter(characterSave);
			
		}
		
		_saveSystem.Save();
		_teamCoordinator =  await InitializeTeamCoordinatorAsync(_config, cancellationToken);

		var testEnemy = await InitializeTestEnemy(cancellationToken);
	}

	private async Task InitializeConfigAsync(CancellationToken token)
	{
		var charactersRemotePage = _scriptableObjectConfig.CharactersRemotePage;
		var skillsRemotePage = _scriptableObjectConfig.SkillsRemotePage;
		var characterTeamPlacesRemotePage = _scriptableObjectConfig.CharacterTeamPlacesRemotePage;
		
		IRemotePage[] remoteDatas = {
			charactersRemotePage,
			skillsRemotePage,
			characterTeamPlacesRemotePage,
		};

		var configParser = new ScriptableObjectConfigParser(remoteDatas, _logger);
		_config = new Config.Config(configParser);

		await _config.InitializeAsync(token);
	}

	private async Task<TeamCoordinatorPresenterBase> InitializeTeamCoordinatorAsync(IConfig config, CancellationToken token)
	{
		var view =
			await _resourceLoader.LoadAndCreateAsync<TeamCoordinatorViewBase, Transform>(
				ResourceIdsContainer.Test.TeamCoordinatorView, _teamCoordinatorParent, token);

		var teamCoordinatorModel = new TeamCoordinatorModel();
		var teamCoordinatorPresenter = new TeamCoordinatorPresenter(
			view,
			teamCoordinatorModel,
			_resourceLoader,
			config,
			_logger,
			_tickHandler,
			_saveSystem,
			_detectionService,
			_movementService,
			_canvasUIContext);

		await teamCoordinatorPresenter.InitializeAsync(token);
		
		return teamCoordinatorPresenter;
	}

	private async Task<EnemyPresenterBase> InitializeTestEnemy(CancellationToken token)
	{
		var testEnemyResourceId= ResourceIdsContainer.Test.TestEnemy;
		var view = await _resourceLoader.LoadAndCreateAsync<EnemyViewBase, Transform>(testEnemyResourceId, _testEnemyParent, token);
		var model = new TestTakeDamageEnemyModel();
		
		var presenter = new TestTakeDamageEnemyPresenter(view, model, _detectionService);
		
		await presenter.InitializeAsync(token);

		return presenter;
	}
}
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DetectionService;
using Code.DungeonTeam.CharacterHealth;
using Code.DungeonTeam.CharacterHealth.Base;
using Code.DungeonTeam.MoveController.Base;
using Code.DungeonTeam.MoveController.VirtualJoystick;
using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.TeamCharacter;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.DungeonTeam.TeamCoordinator.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Generated.Addressables;
using Code.MovementService;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Factory.SkillsPresenter;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.UI.UIContext;
using Disposable.Utils;
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader;
using TickHandler;
using UnityEngine;

namespace Code.DungeonTeam.TeamCoordinator
{
public class TeamCoordinatorPresenter : TeamCoordinatorPresenterBase
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IConfig _config;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;
	private readonly ILocalSaveSystem _saveSystem;
	private readonly IDetectionService _detectionService;
	private readonly IUIContext _uiContext;
	private readonly CharactersConfigPage _charactersConfigPage;
	private readonly List<TeamCharacterPresenterBase> _temCharacters = new();
	private readonly ISkillsPresenterFactory _skillsFactory;
	private readonly List<IHealable> _healableCharacters = new();
	private readonly PlayerTeamSave _playerTeamSave;
	private MoveControllerPresenterBase _moveController;
	private MovementNavigatorPresenterBase _teamMovementNavigator;

	public TeamCoordinatorPresenter(
		TeamCoordinatorViewBase view,
		TeamCoordinatorModelBase model,
		IResourceLoader resourceLoader,
		IConfig config,
		IInGameLogger logger,
		ITickHandler tickHandler,
		ILocalSaveSystem saveSystem,
		IDetectionService detectionService,
		IMovementService movementService,
		IUIContext uiContext) : base(view, model)
	{
		_resourceLoader = resourceLoader;
		_config = config;
		_logger = logger;
		_tickHandler = tickHandler;
		_saveSystem = saveSystem;
		_detectionService = detectionService;
		_uiContext = uiContext;
		_charactersConfigPage = _config.GetConfigPage<CharactersConfigPage>();
		_playerTeamSave = _saveSystem.Load<PlayerTeamSave>();

		var skillDependencies = new SkillDependencies(_tickHandler, movementService);
		_skillsFactory = new SkillsPresenterFactory(skillDependencies, resourceLoader, logger, config, saveSystem);
	}

	protected override async Task OnInitializeAsync(CancellationToken token) {
		_moveController = await InitMoveControllerAsync(token);

		var team = await InitTeamCharactersAsync(token);
		_teamMovementNavigator = await InitializeMovementNavigatorAsync(team, token);
		compositeDisposable.AddDisposable(_teamMovementNavigator);
		
		_temCharacters.AddRange(team);
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		_temCharacters.DisposeAll();
		_temCharacters.Clear();
	}

	private async Task<MovementNavigatorPresenterBase> InitializeMovementNavigatorAsync(
		List<TeamCharacterPresenterBase> team,
		CancellationToken token)
	{
		var moveConfig = _config.GetConfigPage<CharacterTeamMoveConfigPage>();
		var movementNavigatorModelBase = new MovementNavigatorModel(moveConfig, _logger);
		var movementNavigatorViewResourceId = ResourceIdsContainer.Test.MovementNavigatorView;
		var movementNavigatorParent = view.MovementNavigatorParent;
		var navigatorView =
			await _resourceLoader.LoadAndCreateAsync<MovementNavigatorViewBase, Transform>(movementNavigatorViewResourceId,
				movementNavigatorParent, token);
		
		var teamMovementNavigator = new TeamMovementNavigatorPresenter(
			navigatorView,
			movementNavigatorModelBase,
			_logger,
			_tickHandler,
			team, 
			_moveController);

		await teamMovementNavigator.InitializeAsync(token);

		return teamMovementNavigator;
	}

	private async Task<List<TeamCharacterPresenterBase>> InitTeamCharactersAsync(CancellationToken token)
	{
		var playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		var selectedPlayerTeam = playerTeamSave.SelectedPlayerTeam;
		var teamPresenters = new List<TeamCharacterPresenterBase>(selectedPlayerTeam.Count);

		foreach (var characterSave in selectedPlayerTeam.Values)
		{
			var characterSaveId = characterSave.Id;
			var character = await InitTeamCharacterAsync(characterSaveId, characterSave, token);
			
			teamPresenters.Add(character);
			
			if (character is IHealable healableCharacter)
			{
				_healableCharacters.Add(healableCharacter);
			}
			
			compositeDisposable.AddDisposable(character);
		}

		return teamPresenters;
	}
	
	private async Task<MoveControllerPresenterBase> InitMoveControllerAsync(CancellationToken token)
	{
		var uiOverlayParent = _uiContext.UIElementsOverlay;
		var moveControllerViewResourceId = ResourceIdsContainer.GameplayUI.VirtualJoystickView;
		var moveControllerView =
			await _resourceLoader.LoadAndCreateAsync<MoveControllerViewBase, Transform>(moveControllerViewResourceId, uiOverlayParent, token);

		var radius = moveControllerView.ControllerHandleRadius;
		var moveControllerModel = new VirtualJoystickModel(_logger, radius);
		
		var moveController = new VirtualJoystickPresenter(moveControllerView, moveControllerModel);
		
		await moveController.InitializeAsync(token);
		
		return moveController;
	}
	
	private async Task<TeamCharacterPresenterBase> InitTeamCharacterAsync(
		string characterId,
		CharacterSave characterSave,
		CancellationToken token)
	{
		var characterViewResourceId = ResourceIdsContainer.Test.TestHero;
		var charactersParent = view.CharactersParent;
		var characterView =
			await _resourceLoader.LoadAndCreateAsync<TeamCharacterViewBase, Transform>(characterViewResourceId,
				charactersParent, token);

		var characterConfig = _charactersConfigPage.Characters[characterId];
		var characterClass = characterConfig.CharacterClass;
		var attackConfig = characterConfig.AttackConfig;
		var skills = characterConfig.Skills;
		var healSkillsGetTask = GetSkillsByTypeAsync(characterId, skills, token);
		var attackSkillsGetTask = GetSkillsByTypeAsync(characterId, skills, token);
		var characterHealthTask = InitializePlayerCharacterHealth(characterConfig, characterSave, token);
		
		await Task.WhenAll(healSkillsGetTask, attackSkillsGetTask, characterHealthTask);
		var healSkills = healSkillsGetTask.Result;
		var attackSkills = attackSkillsGetTask.Result;
		var characterHealth = characterHealthTask.Result;
		
		var characterModel = new TeamCharacterModel(characterId, characterClass, attackConfig);
		var character = new PlayerTeamCharacterPresenter(
			characterView,
			characterModel,
			_tickHandler,
			_detectionService,
			_logger,
			characterHealth,
			attackSkills,
			healSkills,
			GetNeedToHealAnyCharacter);
		
		await character.InitializeAsync(token);
		
		return character;
	}
	
	private async Task<CharacterHealthPresenterBase> InitializePlayerCharacterHealth(
		CharacterConfig characterConfig,
		CharacterSave characterSave, 
		CancellationToken token)
	{
		var characterHealthViewResourceId = ResourceIdsContainer.Test.CharacterHealthView;
		var uiElementsOverlay = _uiContext.UIElementsOverlay;
		var characterHealthView =
			await _resourceLoader.LoadAndCreateAsync<CharacterHealthViewBase, Transform>(characterHealthViewResourceId,
				uiElementsOverlay, token);

		var healthByLevelConfigs = characterConfig.CharacterHealthByLevelConfig;
		var currentLevel = characterSave.CurrentLevel;
		var currentHealth = characterSave.CurrentHealth;
		var healthModel = new CharacterHealthModel(_logger, healthByLevelConfigs, currentLevel, currentHealth);
		
		var presenter = new CharacterHealthPresenter(characterHealthView, healthModel);
		await presenter.InitializeAsync(token);

		return presenter;
	}

	private async Task<SkillPresenterBase[]> GetSkillsByTypeAsync(string characterId, string[] skillIds, CancellationToken token)
	{
		if(skillIds.Length == 0)
		{
			return Array.Empty<SkillPresenterBase>();
		}
		
		var charactersParent = view.SkillsParent;
    
		var tasks = new Task<SkillPresenterBase>[skillIds.Length];
		for (var i = 0; i < skillIds.Length; i++)
		{
			var skillId = skillIds[i];
			tasks[i] = _skillsFactory.GetAsync(characterId, skillId, charactersParent, token);
		}
    
		var skills = await Task.WhenAll(tasks);

		return skills;
	}
	
	private IHealable GetNeedToHealAnyCharacter()
	{
		foreach (var healableCharacter in _healableCharacters)
		{
			if (!healableCharacter.IsNeedHeal)
			{
				continue;
			}
			
			return healableCharacter;
		}
		
		return null;
	}
}
}
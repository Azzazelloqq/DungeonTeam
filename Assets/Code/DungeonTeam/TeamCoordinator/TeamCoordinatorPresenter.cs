using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DetectionService;
using Code.DungeonTeam.MoveController.Base;
using Code.DungeonTeam.MoveController.VirtualJoystick;
using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.TeamCharacter;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.DungeonTeam.TeamCoordinator.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.MovementService;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Factory.SkillsPresenter;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.Utils.DataContainers;
using Disposable.Utils;
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
	private TeamMovementNavigatorPresenter _teamMovementNavigator;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;
	private readonly ILocalSaveSystem _saveSystem;
	private readonly ResourceMappingData _resourceMappingData;
	private readonly IDetectionService _detectionService;
	private MoveControllerPresenterBase _moveController;
	private readonly CharactersConfigPage _charactersConfigPage;
	private readonly List<TeamCharacterPresenterBase> _temCharacters = new();
	private readonly ISkillsPresenterFactory _skillsFactory;
	private readonly List<IHealable> _healableCharacters;
	
	public TeamCoordinatorPresenter(
		TeamCoordinatorViewBase view,
		TeamCoordinatorModelBase model,
		IResourceLoader resourceLoader,
		IConfig config,
		IInGameLogger logger,
		ITickHandler tickHandler,
		ILocalSaveSystem saveSystem,
		ResourceMappingData resourceMappingData,
		IDetectionService detectionService,
		IMovementService movementService) : base(view, model)
	{
		_resourceLoader = resourceLoader;
		_config = config;
		_logger = logger;
		_tickHandler = tickHandler;
		_saveSystem = saveSystem;
		_resourceMappingData = resourceMappingData;
		_detectionService = detectionService;
		_charactersConfigPage = _config.GetConfigPage<CharactersConfigPage>();
		
		var skillDependencies = new SkillDependencies(_tickHandler, movementService);
		_skillsFactory = new SkillsPresenterFactory(skillDependencies, resourceLoader, logger, config, saveSystem);
	}

	protected override async Task OnInitializeAsync(CancellationToken token) {
		var moveConfig = _config.GetConfigPage<CharacterTeamMoveConfigPage>();
		var movementNavigatorModelBase = new MovementNavigatorModel(moveConfig, _logger);

		_moveController = await InitMoveControllerAsync(token);
		
		var team = await InitTeamCharactersAsync(token);
		_teamMovementNavigator = new TeamMovementNavigatorPresenter(view.MovementNavigatorView,
			movementNavigatorModelBase,
			_logger,
			_tickHandler,
			team, 
			_moveController);

		await _teamMovementNavigator.InitializeAsync(token);
		
		_temCharacters.AddRange(team);
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		_temCharacters.DisposeAll();
		_temCharacters.Clear();
	}

	private async Task<TeamCharacterPresenterBase[]> InitTeamCharactersAsync(CancellationToken token)
	{
		var playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		var teamPresenters = new TeamCharacterPresenterBase[playerTeamSave.SelectedPlayerTeam.Count];

		for (var i = 0; i < playerTeamSave.SelectedPlayerTeam.Count; i++)
		{
			var characterSave = playerTeamSave.SelectedPlayerTeam[i];
			var characterSaveId = characterSave.Id;
			var character = await InitTeamCharacterAsync(characterSaveId, token);
			
			teamPresenters[i] = character;
			
			if (character is IHealable healableCharacter)
			{
				_healableCharacters.Add(healableCharacter);
			}
		}

		return teamPresenters;
	}
	
	private async Task<MoveControllerPresenterBase> InitMoveControllerAsync(CancellationToken token)
	{
		var moveControllerViewResourceId = ResourceIdsContainer.GameplayUI.VirtualJoystickView;
		var moveControllerView =
			await _resourceLoader.LoadResourceAsync<MoveControllerViewBase>(moveControllerViewResourceId, token);
		
		var moveControllerModel = new VirtualJoystickModel();
		var moveController = new VirtualJoystickPresenter(moveControllerView, moveControllerModel);
		
		await moveController.InitializeAsync(token);
		
		return moveController;
	}
	
	private async Task<TeamCharacterPresenterBase> InitTeamCharacterAsync(string characterId, CancellationToken token)
	{
		var characterViewResourceId = _resourceMappingData.GetResourceId(characterId);
		var characterView =
			await _resourceLoader.LoadResourceAsync<TeamCharacterViewBase>(characterViewResourceId, token);

		var characterData = _charactersConfigPage.Characters[characterId];
		var characterClass = characterData.CharacterClass;
		var attackConfig = characterData.AttackConfig;
		var skills = characterData.Skills;
		var healSkillsGetTask = GetSkillsByTypeAsync(characterId, skills, token);
		var attackSkillsGetTask = GetSkillsByTypeAsync(characterId, skills, token);

		await Task.WhenAll(healSkillsGetTask, attackSkillsGetTask);
		var healSkills = healSkillsGetTask.Result;
		var attackSkills = attackSkillsGetTask.Result;
		
		var characterModel = new TeamCharacterModel(characterClass, attackConfig);
		var character = new TeamCharacterPresenter(characterView, characterModel, _tickHandler, _detectionService, _logger,
			attackSkills, healSkills, GetNeedToHealCharacter);
		
		await character.InitializeAsync(token);
		
		return character;
	}

	private async Task<SkillPresenterBase[]> GetSkillsByTypeAsync(string characterId, string[] skillIds, CancellationToken token)
	{
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
	
	private IHealable GetNeedToHealCharacter()
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
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
using Code.SavesContainers;
using Code.SavesContainers.TeamSave;
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
	private MovementNavigatorPresenter _movementNavigator;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;
	private readonly ILocalSaveSystem _saveSystem;
	private readonly ResourceMappingData _resourceMappingData;
	private readonly IDetectionService _detectionService;
	private readonly CharactersConfigPage _charactersConfigPage;
	private readonly SkillsConfigPage _skillsConfigPage;
	private readonly List<TeamCharacterPresenterBase> _temCharacters = new();
	
	public TeamCoordinatorPresenter(
		TeamCoordinatorViewBase view,
		TeamCoordinatorModelBase model,
		IResourceLoader resourceLoader,
		IConfig config,
		IInGameLogger logger,
		ITickHandler tickHandler,
		ILocalSaveSystem saveSystem,
		ResourceMappingData resourceMappingData,
		IDetectionService detectionService) : base(view,
		model) {
		_resourceLoader = resourceLoader;
		_config = config;
		_logger = logger;
		_tickHandler = tickHandler;
		_saveSystem = saveSystem;
		_resourceMappingData = resourceMappingData;
		_detectionService = detectionService;
		_charactersConfigPage = _config.GetConfigPage<CharactersConfigPage>();
		_skillsConfigPage = _config.GetConfigPage<SkillsConfigPage>();
	}

	protected override Task OnInitializeAsync(CancellationToken token) {
		return base.OnInitializeAsync(token);
		
		var moveConfig = _config.GetConfigPage<CharacterTeamMoveConfigPage>();
		var movementNavigatorModelBase = new MovementNavigatorModel(moveConfig, _logger);
		
		var team = InitTeamCharacters();
		//_movementNavigator = new MovementNavigatorPresenter(view.MovementNavigatorView, movementNavigatorModelBase, _logger, _tickHandler, team, );
		_temCharacters.AddRange(team);
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		_temCharacters.DisposeAll();
		_temCharacters.Clear();
	}

	private TeamCharacterPresenterBase[] InitTeamCharacters()
	{
		var playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		
		foreach (var characterId in playerTeamSave.SelectedPlayerTeam)
		{
			
		}

		return null;
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
		var healSkills = GetSkillsByType(skills, SkillType.Heal);
		var attackSkills = GetSkillsByType(skills, SkillType.Attack);
		var characterModel = new TeamCharacterModel(characterClass, attackConfig);
		return null;
		// var character = new TeamCharacterPresenter(characterView, characterModel, _tickHandler, _detectionService, _logger, attackSkills, healSkills,  );
		//
		// await character.InitializeAsync(token);
		//
		// return character;
	}

	private string[] GetSkillsByType(string[] skillIds, SkillType skillType)
	{
		var skillsGroup = _skillsConfigPage.SkillsGroups[skillType];
		
		var count = 0;
		foreach (var skillId in skillIds)
		{
			if (skillsGroup.Skills.ContainsKey(skillId))
			{
				count++;
			}
			else
			{
				_logger.LogError($"Skill {skillId} not found in config by type {skillType}");
			}
		}
		
		var skillsByType = new string[count];
		
		var index = 0;
		foreach (var skillId in skillIds)
		{
			if (skillsGroup.Skills.TryGetValue(skillId, out var skillConfig))
			{
				skillsByType[index++] = skillConfig.Id;
			}
		}

		return skillsByType;
	}
}
}
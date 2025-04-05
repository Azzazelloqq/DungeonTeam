using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DetectionService;
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
using Code.UI.UIContext;
using Disposable.Utils;
using InGameLogger;
using LightDI.Runtime;
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
	private readonly ILocalSaveSystem _saveSystem;
	private readonly CharactersConfigPage _charactersConfigPage;
	private readonly List<TeamCharacterPresenterBase> _temCharacters = new();
	private readonly ISkillsPresenterFactory _skillsFactory;
	private readonly List<IHealable> _healableCharacters = new();
	private readonly PlayerTeamSave _playerTeamSave;
	private MovementNavigatorPresenterBase _teamMovementNavigator;

	public TeamCoordinatorPresenter(
		TeamCoordinatorViewBase view,
		TeamCoordinatorModelBase model,
		[Inject] IResourceLoader resourceLoader,
		[Inject] IConfig config,
		[Inject] IInGameLogger logger,
		[Inject] ILocalSaveSystem saveSystem) : base(view, model)
	{
		_resourceLoader = resourceLoader;
		_config = config;
		_logger = logger;
		_saveSystem = saveSystem;
		_charactersConfigPage = _config.GetConfigPage<CharactersConfigPage>();
		_playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
	}

	protected override async Task OnInitializeAsync(CancellationToken token) {
		var team = await CreateTeamCharactersAsync(token);
		_teamMovementNavigator = await InitializeMovementNavigatorAsync(team, token);
		compositeDisposable.AddDisposable(_teamMovementNavigator);
		
		foreach (var teamCharacter in team)
		{
			await teamCharacter.InitializeAsync(token);
		}

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
		
		var teamMovementNavigator = TeamMovementNavigatorPresenterFactory.CreateTeamMovementNavigatorPresenter(
			navigatorView,
			movementNavigatorModelBase,
			team);

		await teamMovementNavigator.InitializeAsync(token);

		return teamMovementNavigator;
	}

	private async Task<List<TeamCharacterPresenterBase>> CreateTeamCharactersAsync(CancellationToken token)
	{
		var selectedPlayerTeam = _playerTeamSave.SelectedPlayerTeam;
		var teamPresenters = new List<TeamCharacterPresenterBase>(selectedPlayerTeam.Count);

		foreach (var characterSave in selectedPlayerTeam.Values)
		{
			var characterSaveId = characterSave.Id;
			var character = await CreateTeamCharacterAsync(characterSaveId, characterSave, token);
			
			teamPresenters.Add(character);
			
			if (character is IHealable healableCharacter)
			{
				_healableCharacters.Add(healableCharacter);
			}
			
			compositeDisposable.AddDisposable(character);
		}

		return teamPresenters;
	}
	
	private async Task<TeamCharacterPresenterBase> CreateTeamCharacterAsync(
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
		
		var characterLevel = characterSave.CurrentLevel;
		
		var characterModel = new TeamCharacterModel(_logger, characterId, characterClass, attackConfig, characterLevel, skills);
		var character = PlayerTeamCharacterPresenterFactory.CreatePlayerTeamCharacterPresenter(
			characterView,
			characterModel,
			GetNeedToHealAnyCharacter);
		
		return character;
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
using System.Collections.Generic;
using Code.Config;
using Code.DungeonTeam.MoveController.Base;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.Utils.ModelUtils;
using InGameLogger;
using TickHandler;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorPresenter : MovementNavigatorPresenterBase
{
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;
	private readonly MoveControllerPresenterBase _moveController;
	private readonly IConfig _config;
	private readonly List<TeamCharacterPresenterBase> _characters;

	public MovementNavigatorPresenter(
		MovementNavigatorViewBase view,
		MovementNavigatorModelBase model,
		IInGameLogger logger,
		ITickHandler tickHandler,
		TeamCharacterPresenterBase[] characterPresentersBase,
		MoveControllerPresenterBase moveController)
		: base(view, model)
	{
		_logger = logger;
		_tickHandler = tickHandler;
		_moveController = moveController;
		_characters = new List<TeamCharacterPresenterBase>(characterPresentersBase);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();

		var modelCharacters = GetModelCharactersContainers();
		model.InitializeCharacters(modelCharacters);
		
		UpdateTeamNavigationTargets();

		var viewTeamParentPosition = view.TeamParentPosition;
		var teamParentPositionModel = viewTeamParentPosition.ToModelVector();
		model.InitializeTeamParentPosition(teamParentPositionModel);

		_moveController.MoveStarted += OnControllerMoveStarted;
		_moveController.MoveEnded += OnControllerMoveEnded;
		_moveController.DirectionChanged += OnControllerDirectionChanged;
	}

	protected override void OnDispose()
	{
		base.OnDispose();

		_moveController.MoveStarted += OnControllerMoveStarted;
		_moveController.MoveEnded += OnControllerMoveEnded;
		_moveController.DirectionChanged += OnControllerDirectionChanged;
	}

	public override void AddCharacter(TeamCharacterPresenterBase character)
	{
		_characters.Add(character);

		var modelCharacterContainer = GetModelCharacterContainer(character);
		model.AddCharacter(modelCharacterContainer);
		
		UpdateTeamNavigationTargets();
	}

	public override void RemoveCharacter(string characterId)
	{
		RemoveCharacterFromCache(characterId);

		model.RemoveCharacter(characterId);
		
		UpdateTeamNavigationTargets();
	}

	private void RemoveCharacterFromCache(string characterId)
	{
		for (var i = 0; i < _characters.Count; i++)
		{
			var teamCharacterPresenterBase = _characters[i];
			if (teamCharacterPresenterBase.CharacterId != characterId)
			{
				continue;
			}

			_characters.RemoveAt(i);

			break;
		}
	}

	private ModelCharacterContainer[] GetModelCharactersContainers()
	{
		var characters = new ModelCharacterContainer[_characters.Count];
		for (var i = 0; i < _characters.Count; i++)
		{
			characters[i] = GetModelCharacterContainer(_characters[i]);
		}

		return characters;
	}

	private ModelCharacterContainer GetModelCharacterContainer(TeamCharacterPresenterBase character)
	{
		return new ModelCharacterContainer(character.CharacterId, character.CharacterClassType);
	}

	private void OnControllerDirectionChanged(Vector2 direction)
	{
		if (!model.IsMoving)
		{
			_logger.LogError("Can't change direction, because is not moving state");
			return;
		}

		var deltaTime = _tickHandler.DeltaTime;
		var modelDirection = direction.ToModelVector();
		model.MoveTeamByDirection(modelDirection, deltaTime);
		var teamPosition = model.TeamPosition.ToUnityVector();
		
		view.MoveTeamToPosition(teamPosition);
	}

	private void OnControllerMoveEnded()
	{
		model.StopMoveTeamByDirection();

		foreach (var teamCharacterPresenterBase in _characters)
		{
			teamCharacterPresenterBase.OnTeamStay();
		}
	}

	private void OnControllerMoveStarted()
	{
		model.StartMoveTeam();

		foreach (var teamCharacterPresenterBase in _characters)
		{
			teamCharacterPresenterBase.OnTeamMove();
		}
	}

	private void UpdateTeamNavigationTargets()
	{
		foreach (var teamCharacterPresenterBase in _characters)
		{
			var characterId = teamCharacterPresenterBase.CharacterId;
			if (!model.CharacterPlaceNumById.TryGetValue(characterId, out var placeNumber))
			{
				_logger.LogError("Character does not exist in model.");
			}

			var placeTransform = GetPlaceTransform(placeNumber);
			teamCharacterPresenterBase.OnTargetChanged(placeTransform);
		}
	}

	private Transform GetPlaceTransform(int placeNumber)
	{
		foreach (var viewMovementTarget in view.MovementTargets)
		{
			var viewPlaceNumber = viewMovementTarget.PlaceNum;
			if (viewPlaceNumber != placeNumber)
			{
				continue;
			}
			
			var place = viewMovementTarget.Place;

			return place;
		}
		
		_logger.LogError($"Place not found by place number {placeNumber}");

		return null;
	}
}
}
using System;
using System.Collections.Generic;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.TeamCharacter;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Utils.ModelUtils;
using InGameLogger;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorModel : MovementNavigatorModelBase
{
	public override IReadOnlyDictionary<string, int> CharacterPlaceNumById => _characterPlaceNumById;
	public override ModelVector3 TeamPosition => _teamPosition;
	public override bool IsMoving { get; protected set; }
	
	private readonly PlaceConfig[] _placeConfigs;
	private readonly IInGameLogger _logger;
	private readonly Dictionary<string, CharacterClass> _characterClassById = new();
	private readonly Dictionary<string,int> _characterPlaceNumById;
	private readonly float _teamMoveSpeed;
	private string[] _placeAssignments;
	private ModelVector3 _teamPosition;

	public MovementNavigatorModel(CharacterTeamMoveConfigPage characterTeamMoveConfigPage, IInGameLogger logger)
	{
		_placeConfigs = characterTeamMoveConfigPage.PlaceConfigs;
		_teamMoveSpeed = characterTeamMoveConfigPage.TeamSpeed;
		_logger = logger;
		_characterPlaceNumById = new Dictionary<string, int>();
	}

	public override void InitializeCharacters(ModelCharacterContainer[] characters)
	{
		var numPlaces = _placeConfigs.Length;

		_placeAssignments = new string[numPlaces];

		_characterClassById.Clear();
		foreach (var character in characters)
		{
			_characterClassById[character.Id] = character.CharacterClass;
		}

		foreach (var character in characters)
		{
			AssignCharacterToPreferredOrAnyPlace(character);
		}
	}

	public override void InitializeTeamParentPosition(ModelVector3 viewTeamParentPosition)
	{
		_teamPosition = viewTeamParentPosition;
	}

	public override void AddCharacter(ModelCharacterContainer character)
	{
		AssignCharacterToPreferredOrAnyPlace(character);
	}

	public override void RemoveCharacter(string characterId)
	{
		if (!_characterPlaceNumById.TryGetValue(characterId, out var placeNumber))
		{
			_logger.LogError($"Character with ID {characterId} does not exist.");
			return;
		}

		for (var i = 0; i < _placeConfigs.Length; i++)
		{
			if (_placeConfigs[i].PlaceNumber != placeNumber)
			{
				continue;
			}

			_placeAssignments[i] = null;
			break;
		}

		_characterPlaceNumById.Remove(characterId);
		_characterClassById.Remove(characterId);
	}

	public override void StartMoveTeam()
	{
		if (IsMoving)
		{
			_logger.LogError("Move already started.");
			return;
		}
		
		IsMoving = true;
	}

	public override void StopMoveTeamByDirection()
	{
		if (!IsMoving)
		{
			_logger.LogError("Move already ended.");
			return;
		}
		
		IsMoving = false;
	}

	public override void MoveTeamByDirection(ModelVector2 direction, float deltaTime)
	{
		_teamPosition += direction * _teamMoveSpeed * deltaTime;
	}

	private void AssignCharacterToPreferredOrAnyPlace(ModelCharacterContainer character)
	{
		for (var i = 0; i < _placeConfigs.Length; i++)
		{
			if (_placeConfigs[i].PreferredClass != character.CharacterClass)
			{
				continue;
			}

			if (_placeAssignments[i] == null)
			{
				_placeAssignments[i] = character.Id;
				_characterPlaceNumById[character.Id] = _placeConfigs[i].PlaceNumber;
				return;
			}

			var currentOccupantId = _placeAssignments[i];
			var currentOccupantClass = GetCharacterClassById(currentOccupantId);

			if (currentOccupantClass == character.CharacterClass)
			{
				continue;
			}

			_placeAssignments[i] = character.Id;
			_characterPlaceNumById[character.Id] = _placeConfigs[i].PlaceNumber;

			if (AssignDisplacedCharacterToAnyPlace(currentOccupantId))
			{
				return;
			}

			_logger.LogError(
				$"No available place to reassign displaced character with ID {currentOccupantId}");
				
			_characterPlaceNumById.Remove(currentOccupantId);

			return;
		}

		for (var i = 0; i < _placeAssignments.Length; i++)
		{
			if (_placeAssignments[i] != null)
			{
				continue;
			}

			_placeAssignments[i] = character.Id;
			return;
		}

		_logger.LogWarning($"No available place for character with ID {character.Id}");
	}

	private bool AssignDisplacedCharacterToAnyPlace(string characterId)
	{
		for (var i = 0; i < _placeAssignments.Length; i++)
		{
			if (_placeAssignments[i] != null)
			{
				continue;
			}

			_placeAssignments[i] = characterId;
			_characterPlaceNumById[characterId] = _placeConfigs[i].PlaceNumber;
			
			return true;
		}

		return false;
	}

	private CharacterClass GetCharacterClassById(string characterId)
	{
		if (_characterClassById.TryGetValue(characterId, out var characterClass))
		{
			return characterClass;
		}

		_logger.LogException(new Exception($"Character with ID {characterId} not found."));
		
		return CharacterClass.None;
	}
}
}
using System.Collections.Generic;
using Code.Utils.ModelUtils;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator.Base
{
public abstract class MovementNavigatorModelBase : Model
{
	public abstract bool IsMoving { get; protected set; }
	public abstract IReadOnlyDictionary<string, int> CharacterPlaceNumById { get; }
	public abstract ModelVector3 TeamPosition { get; }

	public abstract void InitializeCharacters(ModelCharacterContainer[] characters);
	public abstract void InitializeTeamParentPosition(ModelVector3 viewTeamParentPosition);
	public abstract void AddCharacter(ModelCharacterContainer character);
	public abstract void RemoveCharacter(string characterId);
	public abstract bool StartMoveTeam();
	public abstract bool StopMoveTeamByDirection();
	public abstract void MoveTeamByDirection(ModelVector2 direction, float deltaTime);
}
}
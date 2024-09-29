using MVP;
using UnityEngine;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterViewBase : ViewMonoBehaviour<TeamCharacterPresenterBase>
{
	public abstract void UpdateTargetPlace(Vector3 targetPlace);
}
}
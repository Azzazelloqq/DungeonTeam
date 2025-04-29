using MVP;
using UnityEngine;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterViewBase : ViewMonoBehaviour<TeamCharacterPresenterBase>
{
	public abstract void UpdatePointToFollow(Vector3 targetPosition);
	public abstract void StopFollowToTarget();
	public abstract void UpdateMoveSpeed(float moveSpeed);
	public abstract void PlayMeleeAttackAnimation();
	public abstract Transform SkillsParent { get; }
}
}
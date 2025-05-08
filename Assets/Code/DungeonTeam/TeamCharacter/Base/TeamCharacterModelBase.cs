using System;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Utils.ModelUtils;
using MVP;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterModelBase : Model
{
	public abstract bool IsDead { get; protected set; }
	public abstract bool IsMovingToTarget { get; protected set; }
	public abstract bool IsTargetInAttackRange { get; protected set; }
	public abstract float ViewAngel { get; }
	public abstract float ViewDistance { get; }
	public abstract bool IsTargetInSkillAttackRange { get; protected set; }
	public abstract bool IsTeamMoving { get; protected set; }
	public abstract string CharacterId { get; }
	public abstract CharacterClass HeroClass { get; }
	public abstract int CurrentLevel { get; }
	public abstract int AttackDamage { get; }
	public abstract string[] Skills { get; }
	public abstract bool IsAttackReload { get; }
	public abstract bool IsCanMove { get; }
	public abstract int AttackCastTime { get; }

	public abstract void MoveToTarget();
	public abstract void StopMoveToTarget();
	public abstract void OnTeamMoveStarted();
	public abstract void OnTeamMoveEnded();
	public abstract void CheckAttackDistanceToTarget(ModelVector3 currentPosition, ModelVector3 targetPosition);
	public abstract bool TryAttack(Action attackCallback);
	public abstract void IncreaseLevel();
}
}
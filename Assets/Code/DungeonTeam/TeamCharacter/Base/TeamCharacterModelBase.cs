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
	public abstract int AttackLayer { get; }
	public abstract bool IsTargetInSkillAttackRange { get; protected set; }
	public abstract bool IsTeamMoving { get; protected set; }
	public abstract ModelVector3 Position { get; }
	public abstract string CharacterId { get; }
	public abstract CharacterClass HeroClass { get; }
	public abstract int CurrentLevel { get; protected set; }

	public abstract void MoveToTarget();
	public abstract void UpdateAttackTargetPosition(ModelVector3 targetPosition);
	public abstract void UpdatePosition(ModelVector3 modelPosition);
	public abstract void OnTeamMoveStarted();
	public abstract void OnTeamMoveEnded();
}
}
using MVP;
using Vector3 = Code.ModelStructs.Vector3;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterModelBase : Model
{
    public abstract bool IsDead { get; protected set; }
	public abstract bool IsMovingToAttackTarget { get; protected set; }
	public abstract bool IsTargetInAttackRange { get; protected set; }
	public abstract float ViewAngel { get; }
	public abstract float ViewDistance { get; }
	public abstract int AttackLayer { get; }
	public abstract bool IsTargetInSkillAttackRange { get; protected set; }

	public abstract void MoveToTarget();
	public abstract void UpdateAttackTargetPosition(Vector3 targetPosition);
	public abstract void UpdatePosition(Vector3 modelPosition);
}
}
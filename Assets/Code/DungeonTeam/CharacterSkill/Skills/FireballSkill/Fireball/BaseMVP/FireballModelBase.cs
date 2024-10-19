using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballModelBase : Model
{
	public abstract bool IsActive { get; protected set; }
	public abstract float FireballSpeed { get; }
	public abstract Vector3 TargetPosition { get; }
	public abstract Vector3 CurrentPosition { get; protected set; }
    public abstract bool IsFollowToTarget { get; protected set; }

    public abstract void ChargeFireball();
	public abstract void ActivateFireball();
	public abstract void UpdateTarget(ISkillAttackable skillAttackable);
	public abstract bool IsTargetReached();
	public abstract void UpdatePosition(float frameDeltaTime);
	public abstract void FollowToTarget(Vector3 currentPosition);
	public abstract void FireballExploded();
	public abstract ISkillAttackable GetTarget();
}
}
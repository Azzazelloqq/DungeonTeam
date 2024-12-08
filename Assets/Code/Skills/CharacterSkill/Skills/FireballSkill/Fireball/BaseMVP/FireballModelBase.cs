using Code.Skills.CharacterSkill.Core.SkillAffectable;
using MVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
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
	public abstract void UpdateTarget(IFireballAffectable attackable);
	public abstract bool IsTargetReached();
	public abstract void UpdatePosition(float frameDeltaTime);
	public abstract void FollowToTarget(Vector3 currentPosition);
	public abstract void FireballExploded();
	public abstract IFireballAffectable GetTarget();
}
}
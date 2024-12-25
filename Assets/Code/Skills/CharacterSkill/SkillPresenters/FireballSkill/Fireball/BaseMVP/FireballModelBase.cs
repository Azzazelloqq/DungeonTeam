using MVP;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballModelBase : Model
{
	public abstract float ThresholdToTarget { get; }
	public abstract bool IsActive { get; protected set; }
	public abstract bool IsFollowToTarget { get; protected set; }
	public abstract bool IsFree { get; }
	public abstract bool IsCharging { get; protected set; }
	public abstract float FireballSpeed { get; }

	public abstract void ActivateFireball();
	public abstract void FireballExploded();
	public abstract void OnTargetReached();
	public abstract void ChargeFireball();
	public abstract void StartFollowToTarget();
}
}
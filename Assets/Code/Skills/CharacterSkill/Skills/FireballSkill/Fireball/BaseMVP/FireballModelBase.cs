using Code.Utils.ModelUtils;
using MVP;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballModelBase : Model
{
	public abstract bool IsActive { get; protected set; }
	public abstract float FireballSpeed { get; }
	public abstract ModelVector3 CurrentPosition { get; protected set; }
    public abstract bool IsFollowToTarget { get; protected set; }

    public abstract void ChargeFireball();
	public abstract void ActivateFireball();
	public abstract bool IsTargetReached();
	public abstract void UpdatePosition(ModelVector3 newPosition, ModelVector3 targetPosition);
	public abstract void StartFollowToTarget(ModelVector3 currentPosition);
	public abstract void FireballExploded();
}
}
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball
{
public class FireballModel : FireballModelBase
{
	public override float FireballSpeed { get; }
	public override float ThresholdToTarget => 0.5f;
	public override bool IsActive { get; protected set; }
	public override bool IsFollowToTarget { get; protected set; }
	public override bool IsCharging { get; protected set; }
	public override bool IsFree => !IsActive;

	public FireballModel(float fireballSpeed)
	{
		FireballSpeed = fireballSpeed;
	}

	public override void ActivateFireball()
	{
		IsActive = true;
		IsCharging = false;
	}

	public override void FireballExploded()
	{
		IsActive = false;
	}

	public override void OnTargetReached()
	{
		IsFollowToTarget = false;
	}

	public override void ChargeFireball()
	{
		IsCharging = true;
	}

	public override void StartFollowToTarget()
	{
		IsFollowToTarget = true;
	}
}
}
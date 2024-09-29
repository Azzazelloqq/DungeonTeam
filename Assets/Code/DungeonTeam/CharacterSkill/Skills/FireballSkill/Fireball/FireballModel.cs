using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballModel : FireballModelBase
{
	private const float ThresholdToTarget = 0.5f;

	public override bool IsActive { get; protected set; }
	public override float FireballSpeed { get; }
	public override Vector3 TargetPosition => _currentTarget.GetTargetPosition();

	public override Vector3 CurrentPosition { get; protected set; }
	
	private ISkillAttackable _currentTarget;

	public FireballModel(float fireballSpeed)
	{
		FireballSpeed = fireballSpeed;
	}

	public override void ChargeFireball()
	{
		IsActive = true;
	}

	public override void ActivateFireball()
	{
		IsActive = true;
	}

	public override void UpdateTarget(ISkillAttackable target)
	{
		_currentTarget = target;
	}

	public override void FollowToTarget(Vector3 currentPosition)
	{
		CurrentPosition = currentPosition;
	}

	public override void FireballExploded()
	{
		IsActive = false;
	}

	public override ISkillAttackable GetTarget()
	{
		return _currentTarget;
	}

	public override void UpdatePosition(float frameDeltaTime)
	{
		var direction = (TargetPosition - CurrentPosition).normalized;
		CurrentPosition += direction * FireballSpeed * frameDeltaTime;
	}
	
	public override bool IsTargetReached()
	{
		var distance = Vector3.Distance(CurrentPosition, TargetPosition);
		if (distance <= ThresholdToTarget)
		{
			return true;
		}

		return false;
	}
}
}
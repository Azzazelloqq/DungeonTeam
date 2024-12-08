using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballModel : FireballModelBase
{
	private const float ThresholdToTarget = 0.5f;

	public override bool IsActive { get; protected set; }
	public override float FireballSpeed { get; }
	public override Vector3 TargetPosition => _currentTarget.GetPosition();

	public override Vector3 CurrentPosition { get; protected set; }
    public override bool IsFollowToTarget { get; protected set; }

    private IFireballAffectable _currentTarget;
    private bool _isTargetReached;

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

	public override void UpdateTarget(IFireballAffectable target)
	{
		_currentTarget = target;
	}

	public override void FollowToTarget(Vector3 currentPosition)
	{
		CurrentPosition = currentPosition;
        IsFollowToTarget = true;
        _isTargetReached = false;
    }

	public override void FireballExploded()
	{
		IsActive = false;
	}

	public override IFireballAffectable GetTarget()
	{
		return _currentTarget;
	}

	public override void UpdatePosition(float frameDeltaTime)
	{
		var direction = (TargetPosition - CurrentPosition).normalized;
		CurrentPosition += direction * FireballSpeed * frameDeltaTime;

        _isTargetReached = CheckTargetReached();
        IsFollowToTarget = false;
    }
	
	public override bool IsTargetReached()
    {
        return _isTargetReached;
    }

    private bool CheckTargetReached()
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
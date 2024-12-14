using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Code.Utils.ModelUtils;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballModel : FireballModelBase
{
	private const float ThresholdToTarget = 0.5f;

	public override bool IsActive { get; protected set; }
	public override float FireballSpeed { get; }

	public override ModelVector3 CurrentPosition { get; protected set; }
    public override bool IsFollowToTarget { get; protected set; }

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

	public override void StartFollowToTarget(ModelVector3 currentPosition)
	{
		CurrentPosition = currentPosition;
        IsFollowToTarget = true;
        _isTargetReached = false;
    }

	public override void FireballExploded()
	{
		IsActive = false;
	}

	public override void UpdatePosition(ModelVector3 newPosition, ModelVector3 targetPosition)
	{
		if (!IsFollowToTarget)
		{
			return;
		}
		
		CurrentPosition = newPosition;

        _isTargetReached = CheckTargetReached(targetPosition);

		if (_isTargetReached)
		{
			IsFollowToTarget = false;
		}
	}
	
	public override bool IsTargetReached()
    {
        return _isTargetReached;
    }

    private bool CheckTargetReached(ModelVector3 targetPosition)
    {
        var distance = ModelVector3.Distance(CurrentPosition, targetPosition);
		
        return distance <= ThresholdToTarget;
	}
}
}
using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Code.Utils.ModelUtils;
using TickHandler;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballPresenter : FireballPresenterBase
{
	public override bool IsActive => model.IsActive;
    public override bool IsFollowToTarget => model.IsFollowToTarget;

    private readonly ITickHandler _tickHandler;
	private Action<IFireballAffectable> _onTargetReached;
	private IFireballAffectable _currentTarget;

	public FireballPresenter(FireballViewBase view, FireballModelBase model, ITickHandler tickHandler) : base(view, model)
	{
		_tickHandler = tickHandler;
	}

	public override void Dispose()
	{
		base.Dispose();
		
		_tickHandler.FrameUpdate -= FollowToTarget;
	}

	public override void Activate(IFireballAffectable affectable, Action<IFireballAffectable> onTargetReached)
	{
		_currentTarget = affectable;
		
		model.ActivateFireball();
		view.ActivateFireball();

		_onTargetReached = onTargetReached;

		var modelCurrentPosition = view.CurrentPosition.ToModelVector();
		model.StartFollowToTarget(modelCurrentPosition);
		
		_tickHandler.FrameUpdate -= FollowToTarget;
		_tickHandler.FrameUpdate += FollowToTarget;
	}

	private void FollowToTarget(float deltaTime)
	{
		var targetPosition = _currentTarget.GetPosition().ToModelVector();
		var direction = (targetPosition - model.CurrentPosition).Normalized;
		var newModelPosition = model.CurrentPosition + direction * model.FireballSpeed * deltaTime;
		model.UpdatePosition(newModelPosition, targetPosition);
		
		var viewCurrentPosition = model.CurrentPosition.ToUnityVector();
		view.UpdatePosition(viewCurrentPosition, deltaTime);

		if (!model.IsTargetReached())
		{
			return;
		}

		view.BlowUpFireball();

		_tickHandler.FrameUpdate -= FollowToTarget;
        
        _onTargetReached?.Invoke(_currentTarget);
	}

	public override void ChargeFireball()
	{
		model.ChargeFireball();
		view.ChargeFireball();
	}

	public override void OnBlowUpEffectCompleted()
	{
		model.FireballExploded();
		
		view.HideFireball();
	}
}
}
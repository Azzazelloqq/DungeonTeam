using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using TickHandler;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballPresenter : FireballPresenterBase
{
	public override bool IsActive => model.IsActive;
    public override bool IsFollowToTarget => model.IsFollowToTarget;

    private readonly ITickHandler _tickHandler;
	private Action<IFireballAffectable> _onTargetReached;

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
		model.UpdateTarget(affectable);
		
		model.ActivateFireball();
		view.ActivateFireball();

		_onTargetReached = onTargetReached;

		model.FollowToTarget(view.CurrentPosition);
		
		_tickHandler.FrameUpdate += FollowToTarget;
	}

	private void FollowToTarget(float deltaTime)
	{
		model.UpdatePosition(deltaTime);
		var currentPosition = model.CurrentPosition;
		view.UpdatePosition(currentPosition, deltaTime);

		if (!model.IsTargetReached())
		{
			return;
		}

		view.BlowUpFireball();

		_tickHandler.FrameUpdate -= FollowToTarget;
        var target = model.GetTarget();
        
        _onTargetReached?.Invoke(target);
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
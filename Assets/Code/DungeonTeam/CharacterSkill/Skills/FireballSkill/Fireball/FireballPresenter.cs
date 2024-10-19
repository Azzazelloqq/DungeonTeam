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
	private Action<ISkillAttackable> _onTargetReached;

	public FireballPresenter(FireballViewBase view, FireballModelBase model, ITickHandler tickHandler) : base(view, model)
	{
		_tickHandler = tickHandler;
	}

	public override void Dispose()
	{
		base.Dispose();
		
		_tickHandler.FrameUpdate -= FollowToTarget;
	}

	public override void Activate(ISkillAttackable skillAffectable, Action<ISkillAttackable> onTargetReached)
	{
		model.UpdateTarget(skillAffectable);
		
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
		_onTargetReached?.Invoke(model.GetTarget());
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
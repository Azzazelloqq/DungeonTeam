using System;
using Code.MovementService;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball
{
public class FireballPresenter : FireballPresenterBase
{
	private readonly IMovementService _movementService;
	private readonly IInGameLogger _logger;
	public override bool IsFree => model.IsFree;
    public override bool IsFollowToTarget => model.IsFollowToTarget;

	private Action<IDamageable> _onTargetReached;
	private IDamageable _currentTarget;

	public FireballPresenter(FireballViewBase view, FireballModelBase model, IMovementService movementService, IInGameLogger logger) : base(view, model)
	{
		_movementService = movementService;
		_logger = logger;
	}

	public override void Activate(IDamageable affectable, Action<IDamageable> onTargetReached)
	{
		if (model.IsActive)
		{
			_logger.LogError("Fireball is already active");
			return;
		}
		
		_currentTarget = affectable;
		
		model.ActivateFireball();
		view.ActivateFireball();

		_onTargetReached = onTargetReached;

		var fireballSpeed = model.FireballSpeed;
		var targetTransform = affectable.GetTransform();
		var thresholdToTarget = model.ThresholdToTarget;
		_movementService.StartMoveRigidbodyWithVelocity(
			view.Rigidbody,
			targetTransform,
			fireballSpeed,
			thresholdToTarget,
			OnTargetReached);

		model.StartFollowToTarget();
	}

	private void OnTargetReached()
	{
		model.OnTargetReached();
		view.BlowUpFireball();

        _onTargetReached?.Invoke(_currentTarget);
	}

	public override void ChargeFireball()
	{
		view.ChargeFireball();
		model.ChargeFireball();
	}

	public override void OnBlowUpEffectCompleted()
	{
		model.FireballExploded();
		
		view.HideFireball();
	}
}
}
using System.Collections.Generic;
using Code.MovementService;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;
using Disposable.Utils;
using InGameLogger;
using TickHandler;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill
{
public class BasicFireballSkillPresenter : SkillPresenterBase
{
    public string SkillId => model.SkillId;
	public override bool IsReadyToActivate => _fireballSkill.IsReadyToActivate;

	private readonly ITickHandler _tickHandler;
    private readonly IInGameLogger _logger;
	private readonly ISkill _fireballSkill;
	private readonly List<FireballPresenterBase> _fireballPresentersCash = new();
	private readonly IMovementService _movementService;
	private FireballPresenterBase _chargedFireball;
	private IDamageable _currentTarget;

	public BasicFireballSkillPresenter(
		SkillViewBase view,
		SkillModelBase skillModel,
		ITickHandler tickHandler,
        IInGameLogger logger,
		ISkill fireballSkill,
		IMovementService movementService) : base(view, skillModel)
    {
        _tickHandler = tickHandler;
        _logger = logger;
		_fireballSkill = fireballSkill;
		_movementService = movementService;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		
		_fireballSkill.ChargeCompleted += OnChargeCompleted;
	}

	public override void Dispose()
	{
		base.Dispose();
		
		_fireballPresentersCash.DisposeAll();
		_fireballPresentersCash.Clear();
		_fireballSkill.ChargeCompleted -= OnChargeCompleted;
	}
	
	public override void ActivateSkill(ISkillAffectable target)
	{
		if (target is not IDamageable damageable)
		{
			_logger.LogError($"Target is not {typeof(IDamageable).FullName}");
			
			return;
		}
		
		if (_fireballSkill.IsReadyToActivate)
		{
			return;
		}

		_currentTarget = damageable;
        
		var fireball = CreateFireball();
		fireball.ChargeFireball();
		
		_fireballSkill.StartChargeSkill();
		model.ChargeSkill();
		view.ChargeSkill(); 
	}
	
	private void OnChargeCompleted()
	{
		model.ActivateSkill();
		view.ActivateSkill();
		
		_chargedFireball.Activate(_currentTarget, OnFireballReachTarget);
	}

	private void OnFireballReachTarget(IDamageable target)
	{
		_fireballSkill.Activate(target);
	}

	private FireballPresenterBase CreateFireball()
	{
		foreach (var fireball in _fireballPresentersCash)
		{
			if (!fireball.IsFree)
			{
				continue;
			}

			return fireball;
		}

		var fireballSpeed = view.FireballSpeed;
		var fireballView = view.CreateFireballView();
		var fireballModel = new FireballModel(fireballSpeed);
		var fireballPresenter = new FireballPresenter(fireballView, fireballModel, _movementService, _logger);
		fireballPresenter.Initialize();
		
		_fireballPresentersCash.Add(fireballPresenter);
		
		return fireballPresenter;
	}
}
}
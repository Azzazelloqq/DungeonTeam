using System;
using System.Collections.Generic;
using Code.MovementService;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Base;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Disposable.Utils;
using InGameLogger;
using TickHandler;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillPresenter : FireballSkillPresenterBase
{
    public string SkillId => model.SkillId;

	private readonly ITickHandler _tickHandler;
    private readonly IInGameLogger _logger;
	private readonly ISkill _fireballSkill;
	private readonly List<FireballPresenterBase> _fireballPresentersCash = new();
	private readonly IMovementService _movementService;
	private FireballPresenterBase _chargedFireball;

	public BasicFireballSkillPresenter(
		FireballSkillViewBase view,
		FireballSkillModelBase skillModel,
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

	public void ActivateSkill(IDamageable target)
	{
		if (_fireballSkill.IsReadyToActivate)
		{
			return;
		}
        
		var fireball = CreateFireball();
		fireball.ChargeFireball();
		
		_fireballSkill.StartChargeSkill(target);
		model.ChargeSkill();
		view.ChargeSkill(); 
	}
	
	private void OnChargeCompleted()
	{
		
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
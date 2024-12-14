using System;
using System.Collections.Generic;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.Skills.Attack;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Base;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Disposable.Utils;
using InGameLogger;
using TickHandler;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillPresenter : FireballSkillPresenterBase, IBasicFireballSkill
{
    public event Action ChargeCompleted;
    public string SkillId => model.SkillId;
    public bool IsReadyToActivate => model.IsReadyToActivate;

	private readonly ITickHandler _tickHandler;
    private readonly IInGameLogger _logger;
    private readonly List<FireballPresenterBase> _fireballPresentersCash = new();
    private FireballPresenterBase _chargedFireball;

    public BasicFireballSkillPresenter(
		FireballSkillViewBase view,
		FireballSkillModelBase skillModel,
		ITickHandler tickHandler,
        IInGameLogger logger
        ) : base(view, skillModel)
    {
        _tickHandler = tickHandler;
        _logger = logger;
    }

	public override void Dispose()
	{
		base.Dispose();
		
		_fireballPresentersCash.DisposeAll();
		_fireballPresentersCash.Clear();
	}

    public void StartChargeSkill(IFireballAffectable skillAffectable)
    {
		if (!model.IsReadyToActivate)
		{
			return;
		}
        
		var fireball = CreateFireball();
		fireball.ChargeFireball();
		
		model.ChargeSkill(() => OnChargeCompleted(fireball));
		view.ChargeSkill(); 
    }

	public void Activate(IFireballAffectable skillAffectable)
	{
		if (!model.IsReadyToActivate)
		{
			return;
		}
        
		if (!model.IsReadyToActivate)
		{
			return;
		}
		
		model.ActivateSkill();
		view.ActivateSkill();

		_chargedFireball.Activate(skillAffectable, OnTargetReached);
		_fireballPresentersCash.Add(_chargedFireball);

		_chargedFireball = null;	
	}

	public void CancelActivateSkill()
    {
        model.CancelChargeSkill();
        view.CancelChargeSkill();
    }

    private void OnChargeCompleted(FireballPresenterBase fireball)
    {
        if (_chargedFireball != null)
        {
            _logger.LogError("Charged fireball already exists");
            
            _chargedFireball.Dispose();
            _chargedFireball = null;
        }
        
        _chargedFireball = fireball;
        
        ChargeCompleted?.Invoke();
    }

	private FireballPresenterBase CreateFireball()
	{
		foreach (var fireball in _fireballPresentersCash)
		{
			if (!fireball.IsActive)
			{
				continue;
			}

			return fireball;
		}

		var fireballSpeed = view.FireballSpeed;
		var fireballView = view.CreateFireballView();
		var fireballModel = new FireballModel(fireballSpeed);
		var fireballPresenter = new FireballPresenter(fireballView, fireballModel, _tickHandler);
		fireballPresenter.Initialize();
		
		return fireballPresenter;
	}

	private void OnTargetReached(IFireballAffectable target)
	{
		var damage = model.FireballDamage;
		target.TakeFireballDamage(damage);
	}
}
}
﻿using System;
using System.Collections.Generic;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Attack;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Disposable.Utils;
using InGameLogger;
using TickHandler;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillPresenter : FireballSkillPresenterBase, IBasicFireballSkill
{
    public event Action ChargeCompleted;
    public string Name => model.SkillName;
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


    public void StartChargeSkill(IFireballAffectable skillAttackable)
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
		
		var fireballView = view.CreateFireballView();
		var fireballModel = new FireballModel(model.FireballSpeed);
		var fireballPresenter = new FireballPresenter(fireballView, fireballModel, _tickHandler);
		fireballPresenter.Initialize();
		
		return fireballPresenter;
	}

	private void OnTargetReached(ISkillAttackable target)
	{
		var damage = model.FireballDamage;
		target.Attack(damage);
	}
}
}
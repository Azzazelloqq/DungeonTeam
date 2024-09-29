using System.Collections.Generic;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Attack;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using Disposable.Utils;
using TickHandler;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillPresenter : FireballSkillPresenterBase, IBasicFireballSkill
{
	public string Name => model.SkillName;
	public bool IsReadyToActivate => model.IsReadyToActivate;

	private readonly ITickHandler _tickHandler;
	private readonly List<FireballPresenterBase> _fireballPresentersCash = new();

	public BasicFireballSkillPresenter(
		FireballSkillViewBase view,
		FireballSkillModelBase skillModel,
		ITickHandler tickHandler) : base(view, skillModel)
	{
		_tickHandler = tickHandler;
	}

	public override void Dispose()
	{
		base.Dispose();
		
		_fireballPresentersCash.DisposeAll();
		_fireballPresentersCash.Clear();
	}

	public void Activate(ISkillAttackable skillAttackable)
	{
		if (!model.IsReadyToActivate)
		{
			return;
		}
		
		var fireball = CreateFireball();
		fireball.ChargeFireball();
		
		model.ChargeSkill(() => OnChargeCompleted(fireball, skillAttackable));
		view.ChargeSkill();
	}

	public void UpdateTarget(ISkillAttackable target)
	{
	}

	private void OnChargeCompleted(FireballPresenterBase fireball, ISkillAttackable skillAffectable)
	{
		if (!model.IsReadyToActivate)
		{
			return;
		}
		
		model.ActivateSkill();
		view.ActivateSkill();

		fireball.Activate(skillAffectable, OnTargetReached);
		_fireballPresentersCash.Add(fireball);
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
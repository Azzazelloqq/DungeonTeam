using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Skills.InstantHealSkill.Base;

namespace Code.Skills.CharacterSkill.Skills.InstantHealSkill
{
public class GenericHealPresenter : GenericHealSkillPresenterBase
{
	public override event Action ChargeCompleted;
	public override bool IsReadyToActivate => model.IsCanActivate;

	public GenericHealPresenter(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
	{
	}

	public override void StartChargeSkill(IHealable skillAffectable)
	{
		if (!model.IsCanActivate)
		{
			return;
		}

		view.StartChargeSkill();
		model.StartChargeSkill();
	}

	public override void Activate(IHealable skillAffectable)
	{
		if (!model.IsCanActivate)
		{
			return;
		}

		model.Activate();
		view.ActivateSkill();

		skillAffectable.Heal(HealPoints);
	}

	public override void CancelActivateSkill()
	{
		model.CancelActivateSkill();
		view.CancelActivateSkill();
	}
}
}
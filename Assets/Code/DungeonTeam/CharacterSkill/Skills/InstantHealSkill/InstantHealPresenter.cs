using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill
{
public class InstantHealPresenter : InstantHealSkillPresenterBase
{
	public override event Action ChargeCompleted;
	public override bool IsReadyToActivate => model.IsCanActivate;

	public InstantHealPresenter(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
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
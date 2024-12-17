using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.Skills.HealSkills;
using MVP;

namespace Code.Skills.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class GenericHealSkillPresenterBase : Presenter<InstantHealViewBase, InstantHealSkillModelBase>
{
	public string SkillId => "InstantHeal";
	public abstract event Action ChargeCompleted;
	public abstract bool IsReadyToActivate { get; }

	protected GenericHealSkillPresenterBase(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
	{
	}

	public abstract void StartChargeSkill(IHealable skillAffectable);
	public abstract void Activate(IHealable skillAffectable);
	public int HealPoints => model.HealPoints;
    public abstract void CancelActivateSkill();
}
}
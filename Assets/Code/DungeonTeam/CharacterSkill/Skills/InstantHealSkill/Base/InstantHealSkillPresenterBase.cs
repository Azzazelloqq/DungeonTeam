using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Heal;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealSkillPresenterBase : Presenter<InstantHealViewBase, InstantHealSkillModelBase>, IInstantHealSkill
{
    public abstract event Action ChargeCompleted;
    public string Name => "InstantHeal";

	public int HealPoints => model.HealPoints;

	protected InstantHealSkillPresenterBase(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
	{
	}

    public abstract void StartChargeSkill(IHealable skillAttackable);
    public abstract void Activate(IHealable skillAffectable);
    public abstract void CancelActivateSkill();
}
}
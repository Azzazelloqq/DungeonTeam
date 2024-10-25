﻿using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Heal;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealSkillPresenterBase : Presenter<InstantHealViewBase, InstantHealSkillModelBase>, IInstantHealSkill
{
	public string Name => "InstantHeal";
	public abstract event Action ChargeCompleted;
	public abstract bool IsReadyToActivate { get; }

	protected InstantHealSkillPresenterBase(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
	{
	}

	public abstract void StartChargeSkill(IHealable skillAffectable);
	public abstract void Activate(IHealable skillAffectable);
	public int HealPoints => model.HealPoints;
    public abstract void CancelActivateSkill();
}
}
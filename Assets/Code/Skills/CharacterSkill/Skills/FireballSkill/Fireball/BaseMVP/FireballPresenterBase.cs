﻿using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using MVP;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballPresenterBase : Presenter<FireballViewBase, FireballModelBase>
{
	public FireballPresenterBase(FireballViewBase view, FireballModelBase model) : base(view, model)
	{
	}

	public abstract bool IsActive { get; }
    public abstract bool IsFollowToTarget { get; }

	public abstract void Activate(IFireballAffectable affectable, Action<IFireballAffectable> onTargetReached);
	public abstract void ChargeFireball();
	public abstract void OnBlowUpEffectCompleted();
}
}
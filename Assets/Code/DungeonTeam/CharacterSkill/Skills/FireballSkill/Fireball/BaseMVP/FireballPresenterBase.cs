using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballPresenterBase : Presenter<FireballViewBase, FireballModelBase>
{
	public FireballPresenterBase(FireballViewBase view, FireballModelBase model) : base(view, model)
	{
	}

	public abstract bool IsActive { get; }

	public abstract void Activate(ISkillAttackable skillAffectable, Action<ISkillAttackable> onTargetReached);
	public abstract void ChargeFireball();
	public abstract void OnBlowUpEffectCompleted();
}
}
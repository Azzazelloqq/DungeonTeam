using Code.Skills.CharacterSkill.Core.SkillAffectable;
using MVP;

namespace Code.Skills.CharacterSkill.SkillPresenters.Base
{
public abstract class SkillPresenterBase : Presenter<SkillViewBase, SkillModelBase>
{
	public SkillPresenterBase(SkillViewBase view, SkillModelBase skillModel) : base(view, skillModel)
	{
	}

	public abstract void ActivateSkill(IDamageable target);
}
}
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using MVP;

namespace Code.Skills.CharacterSkill.SkillPresenters.Base
{
public abstract class SkillPresenterBase : Presenter<SkillViewBase, SkillModelBase>
{
	public abstract bool IsReadyToActivate { get; }

	public SkillPresenterBase(SkillViewBase view, SkillModelBase skillModel) : base(view, skillModel)
	{
	}

	public abstract void ActivateSkill(ISkillAffectable target);
}
}
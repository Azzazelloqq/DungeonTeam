using MVP;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillPresenterBase : Presenter<FireballSkillViewBase, FireballSkillModelBase>
{
	public FireballSkillPresenterBase(FireballSkillViewBase view, FireballSkillModelBase skillModel) : base(view, skillModel)
	{
	}
}
}
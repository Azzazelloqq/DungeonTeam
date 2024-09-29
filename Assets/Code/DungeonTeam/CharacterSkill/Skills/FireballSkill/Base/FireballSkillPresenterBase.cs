using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillPresenterBase : Presenter<FireballSkillViewBase, FireballSkillModelBase>
{
	public FireballSkillPresenterBase(FireballSkillViewBase view, FireballSkillModelBase skillModel) : base(view, skillModel)
	{
	}
}
}
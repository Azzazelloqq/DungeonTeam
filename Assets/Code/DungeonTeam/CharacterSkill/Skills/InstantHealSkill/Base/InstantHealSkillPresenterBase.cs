using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Heal;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealSkillPresenterBase : Presenter<InstantHealViewBase, InstantHealSkillModelBase>, IInstantHealSkill
{
	public string Name => "InstantHeal";

	public int HealPoints => model.HealPoints;

	protected InstantHealSkillPresenterBase(InstantHealViewBase view, InstantHealSkillModelBase model) : base(view, model)
	{
	}

	public void Activate(IHealable skillAttackable)
	{
		skillAttackable.Heal(HealPoints);
	}
}
}
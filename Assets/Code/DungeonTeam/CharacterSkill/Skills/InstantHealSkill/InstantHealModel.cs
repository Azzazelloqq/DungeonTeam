using Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill
{
public class InstantHealModel : InstantHealSkillModelBase
{
	public override int HealPoints { get; }

	public InstantHealModel(int healPoints)
	{
		HealPoints = healPoints;
	}
}
}
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Heal
{
public interface IInstantHealSkill : ISkill<IHealable>
{
	public int HealPoints { get; }
	
}
}
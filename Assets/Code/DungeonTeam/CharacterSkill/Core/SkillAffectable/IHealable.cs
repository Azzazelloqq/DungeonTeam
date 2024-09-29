using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.SkillAffectable
{
public interface IHealable : ISkillAffectable
{
	public void Heal(int healPoints);
}
}
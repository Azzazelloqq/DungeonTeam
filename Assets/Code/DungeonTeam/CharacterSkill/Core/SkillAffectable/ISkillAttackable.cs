using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.SkillAffectable
{
public interface ISkillAttackable : ISkillAffectable
{
	public void Attack(float damage);
}
}
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.SkillAffectable
{
public interface IMeleeAttackable : ISkillAffectable
{
	public void TakeCommonAttackDamage(int damage);
}
}
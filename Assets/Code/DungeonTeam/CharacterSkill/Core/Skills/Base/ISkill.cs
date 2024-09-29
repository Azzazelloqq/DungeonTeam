using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
{
public interface ISkill<in TSkillAffectable> where TSkillAffectable : ISkillAffectable
{
	public string Name { get; }
	
	public void Activate(TSkillAffectable skillAttackable);
}
}
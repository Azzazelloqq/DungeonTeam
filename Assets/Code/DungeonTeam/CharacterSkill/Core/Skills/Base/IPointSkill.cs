using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
{
public interface IPointSkill<in TSkillAffectable> : ISkill<TSkillAffectable> where TSkillAffectable : ISkillAffectable
{
	public void UpdatePoint(int x, int y, int z);
}
}
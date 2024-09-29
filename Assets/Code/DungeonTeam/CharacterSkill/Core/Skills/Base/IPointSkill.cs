using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
{
public interface IPointSkill<in TAffectable> : ISkill<TAffectable> where TAffectable : ISkillAffectable
{
	public void UpdatePoint(int x, int y, int z);
}
}
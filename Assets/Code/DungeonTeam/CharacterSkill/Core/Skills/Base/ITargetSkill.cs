using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
{
public interface ITargetSkill<in TAffectable> : ISkill<TAffectable> where TAffectable : ISkillAffectable
{
}
}
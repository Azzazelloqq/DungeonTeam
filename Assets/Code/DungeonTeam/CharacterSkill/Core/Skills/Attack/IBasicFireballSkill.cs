using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Attack
{
public interface IBasicFireballSkill : ITargetSkill<IFireballAffectable>
{
	public bool IsReadyToActivate { get; }
}
}
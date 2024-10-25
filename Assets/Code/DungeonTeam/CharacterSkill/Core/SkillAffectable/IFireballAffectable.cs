using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.SkillAffectable
{
public interface IFireballAffectable : ISkillAffectable
{
    public void TakeFireballDamage(int damage);
}
}
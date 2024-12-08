using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable
{
public interface IFireballAffectable : ISkillAffectable
{
    public void TakeFireballDamage(int damage);
}
}
namespace Code.DungeonTeam.CharacterSkill.Core.SkillAffectable
{
public interface IFireballAffectable : ISkillAttackable
{
    public void TakeFireballDamage(int damage);
}
}
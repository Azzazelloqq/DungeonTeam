using Code.Skills.CharacterSkill.Core.Skills.Base;

namespace Code.Skills.CharacterSkill.Factory.Skills
{
internal interface ISkillsFactory
{
	internal TSkill GetSkill<TSkill>(string skillId, string characterId) where TSkill : ISkill;
}
}
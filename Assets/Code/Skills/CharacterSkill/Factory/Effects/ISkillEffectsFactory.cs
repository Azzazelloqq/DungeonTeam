using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;

namespace Code.Skills.CharacterSkill.Factory.Effects
{
internal interface ISkillEffectsFactory
{
	internal ISkillEffect[] GetSkillEffects(SkillStatsConfig skillConfig);
}
}
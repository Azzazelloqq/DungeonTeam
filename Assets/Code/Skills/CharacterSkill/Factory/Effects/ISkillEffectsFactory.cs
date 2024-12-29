using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;

namespace Code.Skills.CharacterSkill.Factory.Effects
{
/// <summary>
/// Provides methods to retrieve arrays of skill effects based on a given skill configuration.
/// </summary>
internal interface ISkillEffectsFactory
{
	/// <summary>
	/// Returns an array of <see cref="ISkillEffect"/> instances for a specified skill configuration.
	/// </summary>
	/// <param name="skillConfig">
	/// The skill configuration containing data about effects, cooldown, etc.
	/// </param>
	/// <returns>
	/// An array of <see cref="ISkillEffect"/>. Returns an empty array if no effects are configured.
	/// </returns>
	internal ISkillEffect[] GetSkillEffects(SkillStatsConfig skillConfig);
}
}
using Code.Skills.CharacterSkill.Core.Skills.Base;

namespace Code.Skills.CharacterSkill.Factory.Skills
{
/// <summary>
/// Provides methods to retrieve specific skill instances by skill and character identifiers.
/// </summary>
internal interface ISkillsFactory
{
	/// <summary>
	/// Retrieves a skill of type <typeparamref name="TSkill"/> based on the provided skill and character identifiers.
	/// </summary>
	/// <typeparam name="TSkill">
	/// The specific skill type that implements <see cref="ISkill"/>.
	/// </typeparam>
	/// <param name="skillId">The unique identifier of the skill.</param>
	/// <param name="characterId">The unique identifier of the character.</param>
	/// <returns>
	/// Returns an instance of <typeparamref name="TSkill"/> if available,
	/// or the default value if the skill cannot be created.
	/// </returns>
	internal TSkill GetSkill<TSkill>(string skillId, string characterId) where TSkill : ISkill;
}
}
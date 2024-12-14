using TickHandler;

namespace Code.Skills.CharacterSkill.Factory
{
public struct SkillDependencies
{
	public ITickHandler TickHandler { get; }

	public SkillDependencies(ITickHandler tickHandler)
	{
		TickHandler = tickHandler;
	}
}
}
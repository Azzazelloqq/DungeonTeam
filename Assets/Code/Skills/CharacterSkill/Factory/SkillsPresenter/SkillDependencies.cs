using Code.MovementService;
using TickHandler;

namespace Code.Skills.CharacterSkill.Factory.SkillsPresenter
{
public struct SkillDependencies
{
	public ITickHandler TickHandler { get; }
	public IMovementService MovementService { get; } 
	public SkillDependencies(ITickHandler tickHandler, IMovementService movementService)
	{
		TickHandler = tickHandler;
		MovementService = movementService;
	}
}
}
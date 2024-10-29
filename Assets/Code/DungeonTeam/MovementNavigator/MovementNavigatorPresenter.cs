using System.Collections.Generic;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.TeamCharacter.Base;
using TickHandler;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorPresenter : MovementNavigatorPresenterBase
{
	private readonly ITickHandler _tickHandler;
	private List<TeamCharacterPresenterBase> _characters;
	
	public MovementNavigatorPresenter(
		MovementNavigatorViewBase view,
		MovementNavigatorModelBase model,
		ITickHandler tickHandler) : base(view,
		model)
	{
		_tickHandler = tickHandler;
	}
}
}
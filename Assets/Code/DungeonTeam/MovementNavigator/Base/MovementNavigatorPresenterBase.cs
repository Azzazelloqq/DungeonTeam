using Code.DungeonTeam.TeamCharacter.Base;
using MVP;

namespace Code.DungeonTeam.MovementNavigator.Base
{
public abstract class MovementNavigatorPresenterBase : Presenter<MovementNavigatorViewBase, MovementNavigatorModelBase>
{
	protected MovementNavigatorPresenterBase(MovementNavigatorViewBase view, MovementNavigatorModelBase model) : base(view,
		model)
	{
	}

	public abstract void AddCharacter(TeamCharacterPresenterBase character);
	public abstract void RemoveCharacter(string characterId);
}
}
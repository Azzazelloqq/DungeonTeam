using MVP;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterPresenterBase : Presenter<TeamCharacterViewBase, TeamCharacterModelBase> 
{
	protected TeamCharacterPresenterBase(TeamCharacterViewBase view, TeamCharacterModelBase model) : base(view, model)
	{
	}

	public abstract void OnTeamMoveStarted();
	public abstract void OnTeamMoveEnded();
}
}
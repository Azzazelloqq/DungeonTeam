using Code.DungeonTeam.MovementNavigator;
using Code.DungeonTeam.TeamCharacter.Base;
using MVP;

namespace Code.DungeonTeam.TeamCoordinator.Base {
	public abstract class TeamCoordinatorViewBase : ViewMonoBehaviour<TeamCoordinatorPresenterBase> {
		public abstract MovementNavigatorView MovementNavigatorView { get; protected set; }
		public abstract TeamCharacterViewBase CharacterViewPrefab { get; protected set; }
	}
}
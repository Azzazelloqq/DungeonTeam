using MVP;
using UnityEngine;

namespace Code.DungeonTeam.TeamCoordinator.Base {
	public abstract class TeamCoordinatorViewBase : ViewMonoBehaviour<TeamCoordinatorPresenterBase> {
		public abstract Transform SkillsParent { get; protected set; }
		public abstract Transform MovementNavigatorParent { get; protected set; }
		public abstract Transform CharactersParent { get; protected set; }
	}
}
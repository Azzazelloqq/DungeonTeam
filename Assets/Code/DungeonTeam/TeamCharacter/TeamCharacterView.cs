using Code.DungeonTeam.TeamCharacter.Base;
using UnityEngine;
using UnityEngine.AI;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterView : TeamCharacterViewBase
{
	[SerializeField]
	private NavMeshAgent _navMeshAgent;

	public override void UpdateTargetPlace(Vector3 targetPlace)
	{
		_navMeshAgent.SetDestination(targetPlace);
	}
}
}
using Code.DungeonTeam.TeamCharacter.Base;
using UnityEngine;
using UnityEngine.AI;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterView : TeamCharacterViewBase
{
	[SerializeField]
	private NavMeshAgent _navMeshAgent;

	public override void UpdatePointToFollow(Vector3 targetPosition)
	{
		_navMeshAgent.isStopped = false;
		
		_navMeshAgent.SetDestination(targetPosition);
	}

	public override void StopFollowToTarget()
	{
		_navMeshAgent.isStopped = true;
	}
}
}
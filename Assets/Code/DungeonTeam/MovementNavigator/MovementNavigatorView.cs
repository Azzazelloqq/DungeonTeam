using System;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.MovementNavigator.Target;
using UnityEngine;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorView : MovementNavigatorViewBase
{
	[field: SerializeField]
	public override MovementTarget[] MovementTargets { get; protected set; }

	[SerializeField]
	private Transform _targetsParent;

	public override Vector3 TeamParentPosition => _targetsParent.position;

	public override void MoveTeamToPosition(Vector3 teamPosition)
	{
		_targetsParent.position = teamPosition;
	}

	#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		DrowMovementTargets();
	}

	private void DrowMovementTargets()
	{
		if (MovementTargets == null)
		{
			return;
		}

		foreach (var target in MovementTargets)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(target.Place.position, 0.5f);
		}
	}
	#endif
}
}
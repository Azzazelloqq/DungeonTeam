using System.Threading;
using System.Threading.Tasks;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameDebugUtils.CharacterDetection;
using UnityEngine;
using UnityEngine.AI;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterView : TeamCharacterViewBase
{
	[SerializeField]
	private NavMeshAgent _navMeshAgent;

	#if UNITY_EDITOR
	private VisionDebugger _visionDebugger;
	#endif
	
	protected override void OnInitialize()
	{
		base.OnInitialize();
		
		#if UNITY_EDITOR
		InitDebugVision();
		#endif
	}

	protected override async Task OnInitializeAsync(CancellationToken token)
	{
		await base.OnInitializeAsync(token);
		
		#if UNITY_EDITOR
		InitDebugVision();
		#endif
	}

	public override void UpdatePointToFollow(Vector3 targetPosition)
	{
		_navMeshAgent.isStopped = false;
		
		_navMeshAgent.SetDestination(targetPosition);
		
		DrawCharacterVision();
	}

	public override void StopFollowToTarget()
	{
		_navMeshAgent.isStopped = true;
	}

	public override void UpdateMoveSpeed(float moveSpeed)
	{
		_navMeshAgent.speed = moveSpeed;
	}

	private void DrawCharacterVision()
	{
		#if UNITY_EDITOR
		_visionDebugger.UpdateDirection(Vector3.zero, presenter.VisionDirection);
		#endif
	}
	
	#if UNITY_EDITOR
	private void InitDebugVision()
	{
		_visionDebugger = gameObject.AddComponent<VisionDebugger>();
		_visionDebugger.Initialize(presenter.VisionViewAngel, presenter.VisionViewDistance, Color.green);
	}
	#endif
}
}
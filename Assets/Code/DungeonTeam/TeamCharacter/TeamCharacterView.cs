using System.Threading;
using System.Threading.Tasks;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameDebugUtils.CharacterDetection;
using Code.Utils.AnimationUtils;
using UnityEngine;
using UnityEngine.AI;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterView : TeamCharacterViewBase
{
	private static readonly int AttackAnimationName = Animator.StringToHash("Attack");

	[SerializeField]
	private NavMeshAgent _navMeshAgent;

	[SerializeField]
	private ObservableAnimator _mainAnimator;

	[SerializeField]
	private Transform _skillsParent;

	public override Transform SkillsParent => _skillsParent;

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

	public override void PlayMeleeAttackAnimation()
	{
		_mainAnimator.SetTrigger(AttackAnimationName);
	}

	private void DrawCharacterVision()
	{
		#if UNITY_EDITOR
		_visionDebugger.UpdateDirection(Vector3.zero, presenter.VisionDirection);
		#endif
	}

	private void SubscribeOnAnimatorEvents()
	{
		_mainAnimator.AnimationStartedByHash += OnAnimationStartedByHash;
		_mainAnimator.AnimationEndByHash += OnAnimationEndedByHash;
		_mainAnimator.AnimationUpdate += OnAnimationUpdate;
	}

	private void UnsubscribeOnAnimatorEvents()
	{
		_mainAnimator.AnimationStartedByHash -= OnAnimationStartedByHash;
		_mainAnimator.AnimationEndByHash -= OnAnimationEndedByHash;
		_mainAnimator.AnimationUpdate -= OnAnimationUpdate;
	}

	private void OnAnimationStartedByHash(int stateHash)
	{
		if (stateHash == AttackAnimationName)
		{
			OnAttackAnimationStarted();
		}
	}

	private void OnAnimationEndedByHash(int stateHash)
	{
		if (stateHash == AttackAnimationName)
		{
			OnAttackAnimationEnded();
		}
	}

	private void OnAnimationUpdate(AnimatorStateInfo stateInfo)
	{
		var stateHash = stateInfo.shortNameHash;
		if (stateHash == AttackAnimationName)
		{
			OnAttackAnimationUpdated(stateInfo);
		}
	}

	private void OnAttackAnimationStarted()
	{
		presenter.OnAttackAnimationStarted();
	}

	private void OnAttackAnimationUpdated(AnimatorStateInfo stateInfo)
	{
		var normalizedAnimationTime = stateInfo.normalizedTime;
		presenter.OnAttackAnimationUpdated(normalizedAnimationTime);
	}

	private void OnAttackAnimationEnded()
	{
		presenter.OnAttackAnimationEnded();
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
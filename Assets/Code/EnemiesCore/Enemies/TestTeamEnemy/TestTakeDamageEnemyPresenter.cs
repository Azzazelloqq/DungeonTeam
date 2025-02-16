using System.Threading;
using System.Threading.Tasks;
using Code.AI.CharacterBehaviourTree.Trees.Enemy;
using Code.DetectionService;
using Code.EnemiesCore.Enemies.Base.BaseEnemy;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Utils.TransformUtils;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.TestTeamEnemy
{
public class TestTakeDamageEnemyPresenter : EnemyPresenterBase, IDetectable, IDamageable, IEnemyBehaviourTreeAgent
{
	private readonly IDetectionService _detectionService;
	public Vector3 Position => view.transform.position;
	public bool IsDead => model.IsDead;
	public bool IsNeedMoveToEnemyForAttack => false;

	public TestTakeDamageEnemyPresenter(EnemyViewBase view, EnemyModelBase model, IDetectionService detectionService) : base(view, model)
	{
		_detectionService = detectionService;
	}

	protected override async Task OnInitializeAsync(CancellationToken token)
	{
		await base.OnInitializeAsync(token);
		
		_detectionService.RegisterObject(this);
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		_detectionService.UnregisterObject(this);
	}

	public Vector3 GetPosition()
	{
		return Position;
	}

	public ReadOnlyTransform GetTransform()
	{
		return new ReadOnlyTransform(view.transform);
	}

	public void TakeDamage(int damage)
	{
		view.TakeCommonAttackDamage();
		model.TakeCommonAttackDamage(damage);
	}

	public bool IsAvailableUseAttackSkill()
	{
		return false;
	}

	public void AttackEnemy()
	{
		return;
	}

	public bool IsEnemyInAttackRange()
	{
		return false;
	}
	
	public void MoveToEnemyForAttack()
	{
	}

	public void UseAttackSkill()
	{
	}

	public bool TryFindEnemyTarget()
	{
		return false;
	}

	public void Patrol()
	{
	}
}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Code.AI.CharacterBehaviourTree.Trees.Enemy;
using Code.DetectionService;
using Code.EnemiesCore.Enemies.Base.BaseEnemy;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Timer;
using Code.Utils.TransformUtils;
using InGameLogger;
using TickHandler;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.GoblinEnemy
{
public class EnemyPresenter : EnemyPresenterBase, IDetectable, IDamageable, IEnemyBehaviourTreeAgent
{
	private const int TickTreeAgent = 200;

	public Vector3 Position => view.transform.position;
	public bool IsDead => model.IsDead;
	public bool IsAttackSkillCasting => throw new NotImplementedException();
	public bool CanStartAttackSkill => throw new NotImplementedException();
	public bool IsAttackCasting => throw new NotImplementedException();
	public bool CanStartAttack => throw new NotImplementedException();
	public bool IsCanMove => throw new NotImplementedException();
	public string AgentName => view.name;
	public bool IsEnemyReached => throw new NotImplementedException();

	private readonly ITickHandler _tickHandler;
	private readonly IDetectionService _detectionService;
	private readonly EnemyBehaviourTree _enemyBehaviourTree;
	private readonly ActionTimer _aiTickTimer;
	private readonly IInGameLogger _logger;

	public EnemyPresenter(
		EnemyViewBase view,
		EnemyModelBase model,
		ITickHandler tickHandler,
		IDetectionService detectionService,
		IInGameLogger logger) : base(view, model)
	{
		_tickHandler = tickHandler;
		_detectionService = detectionService;
		_logger = logger;

		_enemyBehaviourTree = new EnemyBehaviourTree(this);
		_aiTickTimer = new ActionTimer(_logger);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();

		_detectionService.RegisterObject(this);
		_aiTickTimer.StartLoopTickTimer(TickTreeAgent, _enemyBehaviourTree.Tick);
	}

	protected override Task OnInitializeAsync(CancellationToken token)
	{
		_detectionService.RegisterObject(this);
		_aiTickTimer.StartLoopTickTimer(TickTreeAgent, _enemyBehaviourTree.Tick);

		return base.OnInitializeAsync(token);
	}

	protected override void OnDispose()
	{
		base.OnDispose();

		_detectionService.UnregisterObject(this);
		_aiTickTimer.Dispose();
		_enemyBehaviourTree.Dispose();
	}

	public Vector3 GetPosition()
	{
		return view.transform.position;
	}

	public ReadOnlyTransform GetTransform()
	{
		return new ReadOnlyTransform(view.transform);
	}

	public void TakeDamage(int damage)
	{
		view.TakeCommonAttackDamage();
		model.TakeCommonAttackDamage(damage);

		if (model.IsDead)
		{
			view.StartDieEffect();
		}
	}

	public bool IsAvailableUseAttackSkill()
	{
		throw new NotImplementedException();
	}

	public void AttackEnemy()
	{
		throw new NotImplementedException();
	}

	public bool IsEnemyInAttackRange()
	{
		throw new NotImplementedException();
	}

	public void MoveToEnemyForAttack()
	{
		throw new NotImplementedException();
	}

	public void UseAttackSkill()
	{
		throw new NotImplementedException();
	}

	public void StopMovement()
	{
		throw new NotImplementedException();
	}

	public bool TryFindEnemyTarget()
	{
		throw new NotImplementedException();
	}

	public void Patrol()
	{
		throw new NotImplementedException();
	}
}
}
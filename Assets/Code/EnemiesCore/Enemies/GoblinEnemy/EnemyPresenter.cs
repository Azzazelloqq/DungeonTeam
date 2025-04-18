﻿using System.Threading;
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
        throw new System.NotImplementedException();
    }

    public void AttackEnemy()
    {
        throw new System.NotImplementedException();
    }

    public bool IsEnemyInAttackRange()
    {
        throw new System.NotImplementedException();
    }
    
    public void MoveToEnemyForAttack()
    {
        throw new System.NotImplementedException();
    }

    public void UseAttackSkill()
    {
        throw new System.NotImplementedException();
    }


	public bool TryFindEnemyTarget()
	{
		throw new System.NotImplementedException();
	}

	public void Patrol()
	{
		throw new System.NotImplementedException();
	}
}
}
using Code.AI.CharacterBehaviourTree.Trees.Enemy;
using Code.DetectionService;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.EnemiesCore.Enemies.GoblinEnemy.BaseMVP;
using TickHandler;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.GoblinEnemy
{
public class GoblinEnemyPresenter : GoblinEnemyPresenterBase, IDetectable, IFireballAffectable, IMeleeAttackable, IEnemyBehaviourTreeAgent
{
    private readonly ITickHandler _tickHandler;
    private readonly IDetectionService _detectionService;
    public Vector3 Position => view.transform.position;
    public bool IsDead => model.IsDead;

    public GoblinEnemyPresenter(
        GoblinEnemyViewBase view,
        GoblinEnemyModelBase model,
        ITickHandler tickHandler,
        IDetectionService detectionService) : base(view, model)
    {
        _tickHandler = tickHandler;
        _detectionService = detectionService;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        _detectionService.RegisterObject(this);
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        
        _detectionService.UnregisterObject(this);
    }

    public Vector3 GetPosition()
    {
        return view.transform.position;
    }

    public void TakeCommonAttackDamage(int damage)
    {
        view.TakeCommonAttackDamage();
        model.TakeCommonAttackDamage(damage);
        
        if (model.IsDead)
        {
            view.StartDieEffect();
        }
    }

    public void TakeFireballDamage(int damage)
    {
        view.TakeFireballDamage();
        model.TakeFireballDamage(damage);
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

    public void MoveToEnemy()
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
using System;
using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.DetectionService;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;
using Code.DungeonTeam.CharacterSkill.Core.Skills.Base;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.ModelStructs;
using InGameLogger;
using TickHandler;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Code.DungeonTeam.TeamCharacter
{
//todo: think about separate class by skills type
public class TeamCharacterPresenter : TeamCharacterPresenterBase, ICharacterBehaviourTreeAgent, IDetectable
{
	public Vector3 Position => view.transform.position;
	public bool IsDead => model.IsDead;

	private readonly Transform _target;
	private readonly ITickHandler _tickHandler;
	private readonly IDetectionService _detectionService;
	private readonly IInGameLogger _logger;
	private readonly ISkill<ISkillAffectable>[] _attackSkills;
	private readonly ISkill<ISkillAffectable>[] _healSkills;
	private readonly IHealable[] _allyToHeal;
	private IDetectable _currentTargetToAttack;
	private IHealable _currentTargetToHeal;

	public TeamCharacterPresenter(
		TeamCharacterViewBase view,
		TeamCharacterModelBase model,
		Transform target,
		ITickHandler tickHandler,
        IDetectionService detectionService,
		IInGameLogger logger,
		ISkill<ISkillAffectable>[] attackSkills,
		ISkill<ISkillAffectable>[] healSkills,
		IHealable[] allyToHeal) : base(view,
		model)
	{
		_target = target;
		_tickHandler = tickHandler;
        _detectionService = detectionService;
		_logger = logger;
		_attackSkills = attackSkills;
		_healSkills = healSkills;
		_allyToHeal = allyToHeal;
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

    public bool IsAvailableUseAttackSkill()
	{
		foreach (var attackSkill in _attackSkills)
		{
			if (!attackSkill.IsReadyToActivate)
			{
				continue;
			}

			return true;
		}

		return false;
	}

	public void UseAttackSkill()
	{
		if (_currentTargetToAttack is not ISkillAffectable skillAffectable)
		{
			return;
		}

		foreach (var attackSkill in _attackSkills)
		{
			if (!attackSkill.IsReadyToActivate)
			{
				continue;
			}

			attackSkill.Activate(skillAffectable);
		}
	}

	public bool IsEnemyInAttackRange()
	{
		return model.IsTargetInAttackRange;
	}

	public void MoveToEnemy()
	{
		model.MoveToTarget();
		_tickHandler.FrameUpdate += FollowToAttackTarget;
	}

	private void FollowToAttackTarget(float deltaTime)
	{
		if (model.IsTargetInAttackRange)
		{
			_tickHandler.FrameUpdate -= FollowToAttackTarget;
			return;
		}
		
		var targetPosition = _currentTargetToAttack.Position;
		var modelVector = targetPosition.ToModelVector();
		var modelPosition = view.transform.position.ToModelVector();
		
		model.UpdateAttackTargetPosition(modelVector);
		model.UpdatePosition(modelPosition);
		view.UpdateTargetPlace(targetPosition);
	}

	public void AttackEnemy()
	{
		if (_currentTargetToAttack == null)
		{
			_logger.LogError("Need find target to attack, before attack");
		}
	}

	public bool TryFindTargetToHeal()
	{
		if (!TryGetNeedHealTeamCharacter(out var healable))
		{
			return false;
		}

		_currentTargetToHeal = healable;
		return true;
	}

	public void UseHealSkill()
	{
		if (_healSkills.Length <= 0)
		{
			return;
		}

		if (_currentTargetToHeal == null)
		{
			_logger.LogError("Need find target to heal, before use heal");
			return;
		}

		if (!_currentTargetToHeal.IsNeedHeal)
		{
			_logger.LogError("Target to heal doesn't need heal");
			return;
		}
		
		foreach (var supportSkill in _healSkills)
		{
			if (supportSkill.IsReadyToActivate)
			{
				supportSkill.Activate(_currentTargetToHeal);
			}
		}
	}

	private bool TryGetNeedHealTeamCharacter(out IHealable healable)
	{
		foreach (var teamCharacter in _allyToHeal)
		{
			if (!teamCharacter.IsNeedHeal)
			{
				continue;
			}

			healable = teamCharacter;
			return true;
		}

		healable = null;
		return false;
	}

	public void FollowToDirection()
	{
		throw new NotImplementedException();
	}

	public bool IsNeedFollowToDirection()
	{
		return false;
	}

	public bool TryFindEnemyTarget()
	{
		var heroPosition = view.transform.position;
		var heroForward = view.transform.forward;

		var viewAngel = model.ViewAngel;
		var viewDistance = model.ViewDistance;
		var attackLayer = model.AttackLayer;
		
		var detectedObjects =
			_detectionService.DetectObjectsInView(heroPosition, heroForward, viewAngel, viewDistance, attackLayer);

		if (detectedObjects.Count <= 0)
		{
			return false;
		}

		_currentTargetToAttack = detectedObjects[0];
			
		return true;

	}
}
}
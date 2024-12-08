using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.DetectionService;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Utils.ModelUtils;
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
	public override string CharacterId { get; }
	public override CharacterClass CharacterClassType { get; }

	private readonly ITickHandler _tickHandler;
	private readonly IDetectionService _detectionService;
	private readonly IInGameLogger _logger;
	private readonly ISkill<ISkillAffectable>[] _attackSkills;
	private readonly ISkill<ISkillAffectable>[] _healSkills;
	private readonly IHealable[] _allyToHeal;
	private Transform _teamMoveTarget;
	private IDetectable _currentTargetToAttack;
	private IHealable _currentTargetToHeal;

	public TeamCharacterPresenter(
		TeamCharacterViewBase view,
		TeamCharacterModelBase model,
		ITickHandler tickHandler,
        IDetectionService detectionService,
		IInGameLogger logger,
		ISkill<ISkillAffectable>[] attackSkills,
		ISkill<ISkillAffectable>[] healSkills,
		IHealable[] allyToHeal) : base(view,
		model)
	{
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

	public void FollowToDirection()
	{
		if (!model.IsTeamMoving)
		{
			_logger.LogError("Can't move to target place in team, because is not moving state");
		}
		
		_tickHandler.FrameUpdate += MoveCharacterWithTeam;
	}

	public bool IsNeedFollowToDirection()
	{
		var isNeedFollowToDirection = model.IsTeamMoving;

		if (!isNeedFollowToDirection)
		{
			StopMoveCharacterWithTeam();
		}
		else
		{
			StopStay();
		}
		
		return isNeedFollowToDirection;
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

	public override void OnTargetChanged(Transform target)
	{
		_teamMoveTarget = target;
	}

	public override void OnTeamMove()
	{
		model.OnTeamMoveStarted();
	}

	public override void OnTeamStay()
	{
		model.OnTeamMoveEnded();
	}

	public void Stay()
	{
		if (model.IsTeamMoving)
		{
			_logger.LogError("Can't stay in team, because is moving state");
			
			return;
		}
		
		_tickHandler.FrameUpdate += FollowToStayPlace;
	}
	
	private void StopStay()
	{
		if (!model.IsTeamMoving)
		{
			_logger.LogError("Can't stop stay in team, because is not moving state");
			
			return;
		}
		
		_tickHandler.FrameUpdate -= FollowToStayPlace;
	}

	private void FollowToStayPlace(float deltaTime)
	{
		var targetPosition = _teamMoveTarget.position;
		view.UpdatePointToFollow(targetPosition);
	}

	private void MoveCharacterWithTeam(float deltaTime)
	{
		var targetPosition = _teamMoveTarget.position;
		view.UpdatePointToFollow(targetPosition);
	}

	private void StopMoveCharacterWithTeam()
	{
		_tickHandler.FrameUpdate -= MoveCharacterWithTeam;
		view.StopFollowToTarget();
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
		view.UpdatePointToFollow(targetPosition);
	}
}
}
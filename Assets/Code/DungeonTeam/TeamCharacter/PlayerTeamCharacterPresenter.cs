using System;
using System.Threading;
using System.Threading.Tasks;
using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.BehaviourTree;
using Code.Config;
using Code.DetectionService;
using Code.DungeonTeam.CharacterHealth;
using Code.DungeonTeam.CharacterHealth.Base;
using Code.DungeonTeam.TeamCharacter.Base;
using Code.DungeonTeam.TeamCharacter.Components;
using Code.EnemiesCore.Enemies.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.GameConfig.ScriptableObjectParser.ConfigData.DetectConfig;
using Code.Generated.Addressables;
using Code.MovementService;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Factory.SkillsPresenter;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.Timer;
using Code.UI.UIContext;
using Code.Utils.ModelUtils;
using Code.Utils.TransformUtils;
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader;
using TickHandler;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Code.DungeonTeam.TeamCharacter
{
//todo: think about separate class by skills type
public class PlayerTeamCharacterPresenter : TeamCharacterPresenterBase, ICharacterBehaviourTreeAgent, IDetectable, IHealable
{
	private const int TickTreeAgent = 200;
	
	public Vector3 Position => view.transform.position;
	public bool IsDead => model.IsDead;
	public override string CharacterId => model.CharacterId;
	public override CharacterClass CharacterClassType => model.HeroClass;
	public override float VisionViewAngel => model.ViewAngel;
	public override float VisionViewDistance => model.ViewDistance;
	public override Vector3 VisionDirection => view.transform.forward;
	public bool IsNeedHeal => _characterHealth.IsNeedHeal;
	public bool IsOnTeamPlace => Vector3.Distance(_teamMoveTarget.position, view.transform.position) < 0.1f;

	private readonly ITickHandler _tickHandler;
	private readonly IDetectionService _detectionService;
	private readonly IInGameLogger _logger;
	private readonly IUIContext _uiContext;
	private readonly CharacterTeamMoveConfigPage _moveConfig;
	private readonly LayerMask _obstacleLayerMask;
	private readonly Func<IHealable> _getNeedToHealCharacter;
	private readonly IBehaviourTree _characterBehaviourTree;
	private readonly ActionTimer _aiTickTimer;
	private readonly SkillsPresenterFactory _skillsFactory;
	private readonly CharactersConfigPage _charactersConfigPage;
	private readonly ILocalSaveSystem _saveSystem;
	private readonly IResourceLoader _resourceLoader;
	private readonly CharacterDetectionUpdater _characterDetectionUpdater;
	private SkillPresenterBase[] _healSkills;
	private SkillPresenterBase[] _attackSkills;
	private CharacterHealthPresenterBase _characterHealth;
	private Transform _teamMoveTarget;
	private IDetectable _currentTargetToAttack;
	private IHealable _currentTargetToHeal;

	public PlayerTeamCharacterPresenter(
		TeamCharacterViewBase view,
		TeamCharacterModelBase model,
		ITickHandler tickHandler,
        IDetectionService detectionService,
		IInGameLogger logger,
		IMovementService movementService,
		IResourceLoader resourceLoader,
		IConfig config,
		ILocalSaveSystem saveSystem,
		IUIContext uiContext,
		Func<IHealable> getNeedToHealCharacter) : base(view,
		model)
	{
		_tickHandler = tickHandler;
        _detectionService = detectionService;
		_logger = logger;
		_uiContext = uiContext;
		_moveConfig = config.GetConfigPage<CharacterTeamMoveConfigPage>();
		var detectConfigPage = config.GetConfigPage<DetectConfigPage>();
		_charactersConfigPage = config.GetConfigPage<CharactersConfigPage>();
		_obstacleLayerMask = detectConfigPage.DetectLayerMask;
		_getNeedToHealCharacter = getNeedToHealCharacter;
		_characterBehaviourTree = new CharacterBehaviourTree(this);
		_aiTickTimer = new ActionTimer(_logger);
		_saveSystem = saveSystem;
		_resourceLoader = resourceLoader;
		var skillDependencies = new SkillDependencies(_tickHandler, movementService);
		_skillsFactory = new SkillsPresenterFactory(skillDependencies, resourceLoader, logger, config, saveSystem);
		
		var distanceThreshold = _detectionService.GetCellSize() / 2;
		_characterDetectionUpdater = new CharacterDetectionUpdater(_detectionService, this, _tickHandler, 0.1f, distanceThreshold);
	}

	protected override void OnInitialize()
    {
        base.OnInitialize();
        
        _logger.LogError($"{GetType().Name} doesn't support sync initialization");
	}

	protected override async Task OnInitializeAsync(CancellationToken token)
	{
		await base.OnInitializeAsync(token);
		
		view.UpdateMoveSpeed(_moveConfig.TeamSpeed);
		
		var characterId = model.CharacterId;
		var characterConfig = _charactersConfigPage.Characters[characterId];
		var modelSkills = model.Skills;
		var healSkillsGetTask = GetSkillsByTypeAsync(characterId, modelSkills, token);
		var attackSkillsGetTask = GetSkillsByTypeAsync(characterId, modelSkills, token);
		var playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		var characterSave = playerTeamSave.SelectedPlayerTeam[characterId];
		
		var characterHealthTask = InitializePlayerCharacterHealth(characterConfig, characterSave, token);
		
		await Task.WhenAll(healSkillsGetTask, attackSkillsGetTask, characterHealthTask);
		_healSkills = healSkillsGetTask.Result;
		_attackSkills = attackSkillsGetTask.Result;
		_characterHealth = characterHealthTask.Result;
		
		foreach (var skillPresenterBase in _healSkills)
		{
			await skillPresenterBase.InitializeAsync(token);
		}
		
		foreach (var skillPresenterBase in _attackSkills)
		{
			await skillPresenterBase.InitializeAsync(token);
		}
		
		_characterDetectionUpdater.Initialize();

		_aiTickTimer.StartLoopTickTimer(TickTreeAgent, _characterBehaviourTree.Tick);
	}

	protected override void OnDispose()
    {
        base.OnDispose();
        
		_aiTickTimer.Dispose();
		_characterBehaviourTree.Dispose();
		_characterDetectionUpdater.Dispose();
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

			attackSkill.ActivateSkill(skillAffectable);
			break;
		}
	}

	public bool IsEnemyInAttackRange()
	{
		return model.IsTargetInAttackRange;
	}
	
	public void MoveToEnemyForAttack()
	{
		model.MoveToTarget();
		_tickHandler.FrameUpdate += FollowToAttackTarget;
	}

	public void AttackEnemy()
	{
		if (_currentTargetToAttack == null)
		{
			_logger.LogError("Need find target to attack, before attack");
			return;
		}

		if (_currentTargetToAttack is not IDamageable damageable)
		{
			_logger.LogError("Target to attack is not damageable");
			return;
		}

		var isSuccess = model.TryAttack();
		if (!isSuccess)
		{
			return;
		}

		view.PlayMeleeAttackAnimation();
		
		damageable.TakeDamage(model.AttackDamage);
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
			supportSkill.ActivateSkill(_currentTargetToHeal);
			
			break;
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

	public void ReturnToTeam()
	{
		if (_teamMoveTarget == null)
		{
			_logger.LogError("Can't return to team, because team target is null");
			return;
		}
		
		var targetPosition = _teamMoveTarget.position;
		view.UpdatePointToFollow(targetPosition);
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
		
		if (_currentTargetToAttack != null)
		{
			model.CheckAttackDistanceToTarget(heroPosition.ToModelVector(), _currentTargetToAttack.Position.ToModelVector());
			if (model.IsTargetInAttackRange)
			{
				return true;
			}
		}

		var heroForward = view.transform.forward;

		var viewAngel = model.ViewAngel;
		var viewDistance = model.ViewDistance;
		
		var detectedObjects =
			_detectionService.DetectObjectsInView(heroPosition, heroForward, viewAngel, viewDistance, _obstacleLayerMask);

		if (detectedObjects.Count <= 0)
		{
			return false;
		}
		
		IDetectable closestEnemy = null;
		foreach (var detectable in detectedObjects)
		{
			if (detectable is not IEnemy)
			{
				continue;
			}
			
			if (closestEnemy == null)
			{
				closestEnemy = detectable;
				continue;
			}

			var distanceToClosestEnemy = Vector3.Distance(heroPosition, closestEnemy.Position);
			var distanceToDetectedObject = Vector3.Distance(heroPosition, detectable.Position);

			if (distanceToDetectedObject < distanceToClosestEnemy)
			{
				closestEnemy = detectable;
			}
		}

		if (closestEnemy == null)
		{
			return false;
		}

		_currentTargetToAttack = closestEnemy;
			
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

	public Vector3 GetPosition()
	{
		return view.transform.position;
	}

	public ReadOnlyTransform GetTransform()
	{
		return new ReadOnlyTransform(view.transform);
	}

	public void Heal(int healPoints)
	{
		_characterHealth.Heal(healPoints);
	}

	private async Task<SkillPresenterBase[]> GetSkillsByTypeAsync(string characterId, string[] skillIds, CancellationToken token)
	{
		if(skillIds.Length == 0)
		{
			return Array.Empty<SkillPresenterBase>();
		}
		
		var charactersParent = view.SkillsParent;
    
		var tasks = new Task<SkillPresenterBase>[skillIds.Length];
		for (var i = 0; i < skillIds.Length; i++)
		{
			var skillId = skillIds[i];
			tasks[i] = _skillsFactory.GetAsync(characterId, skillId, charactersParent, token);
		}
    
		var skills = await Task.WhenAll(tasks);

		return skills;
	}
	
	private async Task<CharacterHealthPresenterBase> InitializePlayerCharacterHealth(
		CharacterConfig characterConfig,
		CharacterSave characterSave, 
		CancellationToken token)
	{
		var characterHealthViewResourceId = ResourceIdsContainer.Test.CharacterHealthView;
		var uiElementsOverlay = _uiContext.UIElementsOverlay;
		var characterHealthView =
			await _resourceLoader.LoadAndCreateAsync<CharacterHealthViewBase, Transform>(characterHealthViewResourceId,
				uiElementsOverlay, token);

		var healthByLevelConfigs = characterConfig.CharacterHealthByLevelConfig;
		var currentLevel = characterSave.CurrentLevel;
		var currentHealth = characterSave.CurrentHealth;
		var healthModel = new CharacterHealthModel(_logger, healthByLevelConfigs, currentLevel, currentHealth);
		
		var presenter = new CharacterHealthPresenter(characterHealthView, healthModel);
		await presenter.InitializeAsync(token);

		return presenter;
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

	private void StopMoveCharacterWithTeam()
	{
		_tickHandler.FrameUpdate -= MoveCharacterWithTeam;
		view.StopFollowToTarget();
	}

	private void MoveCharacterWithTeam(float deltaTime)
	{
		var targetPosition = _teamMoveTarget.position;

		view.UpdatePointToFollow(targetPosition);
	}

	private bool TryGetNeedHealTeamCharacter(out IHealable healable)
	{
		healable = _getNeedToHealCharacter.Invoke();
		
		return healable != null;
	}

	private void FollowToAttackTarget(float deltaTime)
	{
		if (model.IsTargetInAttackRange)
		{
			_tickHandler.FrameUpdate -= FollowToAttackTarget;
			return;
		}
		
		var targetPosition = _currentTargetToAttack.Position;
		var position = view.transform.position;

		model.CheckAttackDistanceToTarget(position.ToModelVector(), targetPosition.ToModelVector());
		
		view.UpdatePointToFollow(targetPosition);
	}
}
}
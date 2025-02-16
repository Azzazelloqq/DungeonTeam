using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Timer;
using Code.Utils.ModelUtils;
using InGameLogger;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterModel : TeamCharacterModelBase
{
	public override CharacterClass HeroClass { get; }
	public override string[] Skills { get; }
	public override int CurrentLevel => _currentLevel;
	public override int AttackDamage => _attackInfoConfig.AttackDamage;
	public override bool IsDead { get; protected set; }
	public override bool IsMovingToTarget { get; protected set; }
	public override bool IsTargetInAttackRange { get; protected set; }
	public override bool IsTargetInSkillAttackRange { get; protected set; }
	public override bool IsTeamMoving { get; protected set; }
	public override float ViewDistance { get; }
	public override float ViewAngel { get; }
	public override string CharacterId { get; }
	private bool IsCanAttack => !_attackReloadTimer.IsInProgress;

	private readonly float _attackSkillDistance;
	private readonly float _attackDistance;
	private readonly CharacterAttackConfig _attackMainConfig;
	private readonly ActionTimer _attackReloadTimer;
	private readonly IInGameLogger _logger;
	private CharacterAttack _attackInfoConfig;
	private int _currentLevel;

	public TeamCharacterModel(
		IInGameLogger logger,
		string id,
		CharacterClass heroClass,
		CharacterAttackConfig attackConfig,
		int currentLevel,
		string[] skills)
	{
		CharacterId = id;
		HeroClass = heroClass;
		Skills = skills;
		_logger = logger;
		_currentLevel = currentLevel;
		ViewDistance = attackConfig.ViewDistance;
		ViewAngel = attackConfig.ViewAngel;
		_attackSkillDistance = attackConfig.AttackSkillDistance;
		_attackDistance = attackConfig.AttackDistance;
		_attackInfoConfig = attackConfig.AttackByLevels[_currentLevel];
		_attackMainConfig = attackConfig;
		_attackReloadTimer = new ActionTimer(_logger);
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		_attackReloadTimer.Dispose();
	}

	public override void MoveToTarget()
	{
		IsMovingToTarget = true;
	}
	
	public override void StopMoveToTarget()
	{
		IsMovingToTarget = false;
	}

	public override void OnTeamMoveStarted()
	{
		IsTeamMoving = true;
	}

	public override void OnTeamMoveEnded()
	{
		IsTeamMoving = false;
	}

	public override void IncreaseLevel()
	{
		_currentLevel++;
		_attackInfoConfig = _attackMainConfig.AttackByLevels[_currentLevel];
	}

	public override void CheckAttackDistanceToTarget(ModelVector3 currentPosition, ModelVector3 targetPosition)
	{
		var distanceToAttackTarget = ModelVector3.Distance(currentPosition, targetPosition);

		IsTargetInAttackRange = distanceToAttackTarget <= _attackDistance;

		IsTargetInSkillAttackRange = distanceToAttackTarget <= _attackSkillDistance;
	}

	public override bool TryAttack()
	{
		if (!IsCanAttack)
		{
			return false;
		}

		var reloadAttackPerMilliseconds = _attackInfoConfig.ReloadAttackPerMilliseconds;
		_attackReloadTimer.StartTimer(reloadAttackPerMilliseconds);
		
		return true;
	}
}
}
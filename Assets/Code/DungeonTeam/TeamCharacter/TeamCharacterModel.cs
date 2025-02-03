using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Utils.ModelUtils;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterModel : TeamCharacterModelBase
{
	public override CharacterClass HeroClass { get; }
	public override int CurrentLevel { get; protected set; }
	public override bool IsDead { get; protected set; }
	public override bool IsMovingToTarget { get; protected set; }
	public override bool IsTargetInAttackRange { get; protected set; }
	public override bool IsTargetInSkillAttackRange { get; protected set; }
	public override bool IsTeamMoving { get; protected set; }
	public override int AttackLayer { get; }
	public override float ViewDistance { get; }
	public override float ViewAngel { get; }
	public override ModelVector3 Position => _currentPosition;
	public override string CharacterId { get; }

	private readonly float _attackSkillDistance;
	private readonly float _attackDistance;
	
	private ModelVector3 _currentPosition;
	private ModelVector3 _attackTargetPosition;

	public TeamCharacterModel(
		string id,
		CharacterClass heroClass,
		CharacterAttackConfig attackConfig)
	{
		CharacterId = id;
		HeroClass = heroClass;
		AttackLayer = attackConfig.AttackLayer;
		ViewDistance = attackConfig.ViewDistance;
		ViewAngel = attackConfig.ViewAngel;
		_attackSkillDistance = attackConfig.AttackSkillDistance;
		_attackDistance = attackConfig.AttackDistance;
	}

	public override void MoveToTarget()
	{
		IsMovingToTarget = true;
	}

	public override void UpdateAttackTargetPosition(ModelVector3 targetPosition)
	{
		_attackTargetPosition = targetPosition;
		
		CheckDistanceToTarget();
	}

	public override void UpdatePosition(ModelVector3 modelPosition)
	{
		_currentPosition = modelPosition;

		CheckDistanceToTarget();
	}

	public override void OnTeamMoveStarted()
	{
		IsTeamMoving = true;
	}

	public override void OnTeamMoveEnded()
	{
		IsTeamMoving = false;
	}

	private void CheckDistanceToTarget()
	{
		var distanceToAttackTarget = ModelVector3.Distance(_currentPosition, _attackTargetPosition);

		if (distanceToAttackTarget <= _attackDistance)
		{
			IsTargetInAttackRange = true;
			IsMovingToTarget = false;
		}

		if (distanceToAttackTarget <= _attackSkillDistance)
		{
			IsTargetInSkillAttackRange = true;
			IsMovingToTarget = false;
		}
	}
}
}
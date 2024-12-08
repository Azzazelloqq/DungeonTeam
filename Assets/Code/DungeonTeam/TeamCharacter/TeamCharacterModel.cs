using Code.DungeonTeam.TeamCharacter.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.Utils.ModelUtils;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterModel : TeamCharacterModelBase
{
	public CharacterClass HeroClass { get; }
	public override bool IsDead { get; protected set; }
	public override bool IsMovingToAttackTarget { get; protected set; }
	public override bool IsTargetInAttackRange { get; protected set; }
	public override bool IsTargetInSkillAttackRange { get; protected set; }
	public override bool IsTeamMoving { get; protected set; }
	public override int AttackLayer { get; }
	public override float ViewDistance { get; }
	public override float ViewAngel { get; }

	private readonly float _attackSkillDistance;
	private readonly float _attackDistance;
	
	private ModelVector3 _currentPosition;
	private ModelVector3 _attackTargetPosition;

	public TeamCharacterModel(
		CharacterClass heroClass,
		int attackLayer, 
		float viewDistance,
		float viewAngel,
		float attackSkillDistance,
		float attackDistance)
	{
		HeroClass = heroClass;
		AttackLayer = attackLayer;
		ViewDistance = viewDistance;
		ViewAngel = viewAngel;
		_attackSkillDistance = attackSkillDistance;
		_attackDistance = attackDistance;
	}

	public override void MoveToTarget()
	{
		IsMovingToAttackTarget = true;
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
			IsMovingToAttackTarget = false;
		}

		if (distanceToAttackTarget <= _attackSkillDistance)
		{
			IsTargetInSkillAttackRange = true;
			IsMovingToAttackTarget = false;
		}
	}
}
}
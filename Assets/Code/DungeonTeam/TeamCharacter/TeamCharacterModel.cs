using Code.DungeonTeam.TeamCharacter.Base;
using Vector3 = Code.ModelStructs.Vector3;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterModel : TeamCharacterModelBase
{
	public CharacterClass HeroClass { get; }
	public override bool IsDead { get; protected set; }
	public override bool IsMovingToAttackTarget { get; protected set; }
	public override bool IsTargetInAttackRange { get; protected set; }
	public override bool IsTargetInSkillAttackRange { get; protected set; }
	public override int AttackLayer { get; }
	public override float ViewDistance { get; }
	public override float ViewAngel { get; }

	private readonly float _attackSkillDistance;
	private readonly float _attackDistance;
	
	private Vector3 _currentPosition;
	private Vector3 _attackTargetPosition;

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

	public override void UpdateAttackTargetPosition(Vector3 targetPosition)
	{
		_attackTargetPosition = targetPosition;
		
		CheckDistanceToTarget();
	}

	public override void UpdatePosition(Vector3 modelPosition)
	{
		_currentPosition = modelPosition;

		CheckDistanceToTarget();
	}

	private void CheckDistanceToTarget()
	{
		var distanceToAttackTarget = Vector3.Distance(_currentPosition, _attackTargetPosition);

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
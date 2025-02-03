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
	public override string CharacterId { get; }

	private readonly float _attackSkillDistance;
	private readonly float _attackDistance;
	

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

	public override void CheckAttackDistanceToTarget(ModelVector3 currentPosition, ModelVector3 targetPosition)
	{
		var distanceToAttackTarget = ModelVector3.Distance(currentPosition, targetPosition);

		if (distanceToAttackTarget <= _attackDistance)
		{
			IsTargetInAttackRange = true;
		}

		if (distanceToAttackTarget <= _attackSkillDistance)
		{
			IsTargetInSkillAttackRange = true;
		}
	}
}
}
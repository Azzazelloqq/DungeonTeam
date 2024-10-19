using Code.AI.CharacterBehaviourTree;
using Code.DungeonTeam.TeamCharacter.Base;
using TickHandler;
using UnityEngine;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterPresenter : TeamCharacterPresenterBase, ICharacterBehaviourTreeAgent
{
	private readonly Transform _target;
	private readonly ITickHandler _tickHandler;

	public TeamCharacterPresenter(
		TeamCharacterViewBase view,
		TeamCharacterModelBase model,
		Transform target,
		ITickHandler tickHandler) : base(view,
		model)
	{
		_target = target;
		_tickHandler = tickHandler;
	}

	public bool IsAvailableAttackSkill()
	{
		throw new System.NotImplementedException();
	}

	public void UseSkill()
	{
		throw new System.NotImplementedException();
	}

	public bool IsEnemyInSight()
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

	public void AttackEnemy()
	{
		throw new System.NotImplementedException();
	}

	public bool IsAllyNeedSupport()
	{
		throw new System.NotImplementedException();
	}

	public void UseSupportSkill()
	{
		throw new System.NotImplementedException();
	}

	public void FollowToDirection()
	{
		throw new System.NotImplementedException();
	}
}
}
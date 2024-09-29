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
	private readonly TeamCharacterViewBase _view;
	private readonly TeamCharacterModelBase _model;

	public TeamCharacterPresenter(
		TeamCharacterViewBase view,
		TeamCharacterModelBase model,
		Transform target,
		ITickHandler tickHandler) : base(view,
		model)
	{
		_target = target;
		_tickHandler = tickHandler;

		_model = model;
		_view = view;
	}

	protected override void OnInitialize()
	{
		_model.Initialize();
		_view.Initialize(this);
		
		_tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
	}

	public override void Dispose()
	{
		base.Dispose();
		
		_model.Dispose();
		_view.Dispose();
		
		_tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
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
	
	private void OnFrameUpdate(float deltaTime)
	{
		_view.UpdateTargetPlace(_target.position);
	}
}
}
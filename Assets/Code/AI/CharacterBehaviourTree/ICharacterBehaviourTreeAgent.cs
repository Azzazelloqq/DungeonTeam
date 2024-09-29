using System;

namespace Code.AI.CharacterBehaviourTree
{
public interface ICharacterBehaviourTreeAgent : IDisposable
{
	public bool IsAvailableAttackSkill();
	public void UseSkill();
	public bool IsEnemyInSight();
	public bool IsEnemyInAttackRange();
	public void MoveToEnemy();
	public void AttackEnemy();
	public bool IsAllyNeedSupport();
	public void UseSupportSkill();
	public void FollowToDirection();
}
}
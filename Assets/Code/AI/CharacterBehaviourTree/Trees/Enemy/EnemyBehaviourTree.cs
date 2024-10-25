﻿using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes;
using Code.AI.CharacterBehaviourTree.Trees.Character;
using Code.BehaviourTree;
using Code.BehaviourTree.Nodes;

namespace Code.AI.CharacterBehaviourTree.Trees.Enemy
{
public class EnemyBehaviourTree : IBehaviourTree
{
   private readonly IBehaviourTreeNode _root;

	public EnemyBehaviourTree(IEnemyBehaviourTreeAgent agent)
	{
		var attackEnemySequence = InitAttackEnemyNode(agent);
		var followEnemySequence = InitFollowEnemyNode(agent);

		var combatSelector = new SelectorNode(new[]{
			attackEnemySequence,
			followEnemySequence
		});

		var patrolNode = InitPatrolNode(agent);

		_root = new SelectorNode(new[]{
			combatSelector,
			patrolNode,
		});
	}

	public void Tick()
	{
		_root.Tick();
	}

	private IBehaviourTreeNode InitAttackEnemyNode(IEnemyBehaviourTreeAgent agent)
	{
		var useAttackSkillNode = InitUseAttackSkillNode(agent);
		var findTargetNode = new FindEnemyTargetInSightNode(agent);
		var isEnemyInAttackRange = new IsEnemyInAttackRangeNode(agent);
		var attackEnemy = new AttackEnemyNode(agent);
		
		var attackSelector = new SelectorNode(new[]{
			useAttackSkillNode,
			attackEnemy,
		});

		var attackEnemySequence = new SequenceNode(new IBehaviourTreeNode[]{
			findTargetNode,
			isEnemyInAttackRange,
			attackSelector
		});

		return attackEnemySequence;
	}

	private IBehaviourTreeNode InitUseAttackSkillNode(IEnemyBehaviourTreeAgent agent)
	{
		var isAvailableSkill = new IsAvailableUseAttackSkillNode(agent);
		var useSkill = new UseAttackSkillNode(agent);

		var useAttackSkillSequence = new SequenceNode(new IBehaviourTreeNode[]{
			isAvailableSkill,
			useSkill
		});

		return useAttackSkillSequence;
	}

	private IBehaviourTreeNode InitFollowEnemyNode(IEnemyBehaviourTreeAgent agent)
	{
		var moveToEnemyNode = new MoveToEnemyNode(agent);

		var chaseEnemySequence = new SequenceNode(new IBehaviourTreeNode[]{ moveToEnemyNode });

		return chaseEnemySequence;
	}

	private IBehaviourTreeNode InitPatrolNode(IEnemyBehaviourTreeAgent agent)
	{
		var patrolNode = new PatrolNode(agent);

		return patrolNode;
	}
}
}
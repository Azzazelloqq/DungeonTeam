using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes;
using Code.BehaviourTree;
using Code.BehaviourTree.Nodes;

namespace Code.AI.CharacterBehaviourTree
{
public class CharacterBehaviourTree : IBehaviourTree
{
	private readonly IBehaviourTreeNode _root;

	public CharacterBehaviourTree(ICharacterBehaviourTreeAgent agent)
	{
		var useAttackSkillSequence = InitUseAttackSkillNode(agent);
		
		var attackEnemySequence = InitAttackEnemyNode(agent);

		var chaseEnemySequence = InitChaseEnemyNode(agent);

		var combatSelector = new Selector(new[]
		{
			attackEnemySequence,
			chaseEnemySequence,
		});

		var enemySequence = new Sequence(new IBehaviourTreeNode[]
		{
			new IsEnemyInSightNode(agent),
			combatSelector,
		});

		var supportSequence = InitSupportNode(agent);

		var followTeamDirectionNode = InitFollowTeamDirectionNode(agent);

		_root = new Selector(new[]
		{
			useAttackSkillSequence,
			supportSequence,
			enemySequence,
			followTeamDirectionNode,
		});
	}

	public void Tick()
	{
		_root.Tick();
	}

	private IBehaviourTreeNode InitUseAttackSkillNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAvailableSkill = new IsAvailableAttackSkillNode(agent);
		var useSkill = new UseAttackSkillNode(agent);
		
		var useAttackSkillSequence = new Sequence(new IBehaviourTreeNode[]
		{
			isAvailableSkill,
			useSkill,
		});

		return useAttackSkillSequence;
	}

	private IBehaviourTreeNode InitAttackEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		var isEnemyInSight = new IsEnemyInSightNode(agent);
		var isEnemyInAttackRange = new IsEnemyInAttackRangeNode(agent);
		var attackEnemy = new AttackEnemyNode(agent);

		var attackEnemySequence = new Sequence(new IBehaviourTreeNode[]
		{
			isEnemyInSight,
			isEnemyInAttackRange,
			attackEnemy
		});

		return attackEnemySequence;
	}

	private IBehaviourTreeNode InitChaseEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		var moveToEnemyNode = new MoveToEnemyNode(agent);

		var chaseEnemySequence = new Sequence(new IBehaviourTreeNode[] { moveToEnemyNode });

		return chaseEnemySequence;
	}

	private IBehaviourTreeNode InitSupportNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAllyNeedSupport = new IsAllyNeedSupportNode(agent);
		var useSupportSkill = new UseSupportSkillNode(agent);
		var supportSequence = new Sequence(new IBehaviourTreeNode[] { isAllyNeedSupport, useSupportSkill });

		return supportSequence;
	}

	private IBehaviourTreeNode InitFollowTeamDirectionNode(ICharacterBehaviourTreeAgent agent)
	{
		var followTeamDirectionTargetNode = new FollowTeamDirectionTargetNode(agent);

		return followTeamDirectionTargetNode;
	}
}
}
using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes;
using Code.BehaviourTree;
using Code.BehaviourTree.Nodes;

namespace Code.AI.CharacterBehaviourTree.Trees.Character
{
public class CharacterBehaviourTree : IBehaviourTree
{
	private readonly IBehaviourTreeNode _root;

	public CharacterBehaviourTree(ICharacterBehaviourTreeAgent agent)
	{
		var attackEnemySequence = InitAttackEnemyNode(agent);
		var chaseEnemySequence = InitFollowEnemyNode(agent);

		var combatSelector = new SelectorNode(new[]{
			attackEnemySequence,
			chaseEnemySequence
		});

		var supportSequence = InitHealNode(agent);
		var followTeamDirectionNode = InitFollowTeamDirectionNode(agent);

		_root = new SelectorNode(new[]{
			followTeamDirectionNode,
			supportSequence,
			combatSelector,
		});
	}

	public void Tick()
	{
		_root.Tick();
	}

	private IBehaviourTreeNode InitAttackEnemyNode(ICharacterBehaviourTreeAgent agent)
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

	private IBehaviourTreeNode InitUseAttackSkillNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAvailableSkill = new IsAvailableUseAttackSkillNode(agent);
		var useSkill = new UseAttackSkillNode(agent);

		var useAttackSkillSequence = new SequenceNode(new IBehaviourTreeNode[]{
			isAvailableSkill,
			useSkill
		});

		return useAttackSkillSequence;
	}

	private IBehaviourTreeNode InitFollowEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		var moveToEnemyNode = new MoveToEnemyNode(agent);

		var chaseEnemySequence = new SequenceNode(new IBehaviourTreeNode[]{ moveToEnemyNode });

		return chaseEnemySequence;
	}

	private IBehaviourTreeNode InitHealNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAllyNeedSupport = new FindTargetToHealNode(agent);
		var useSupportSkill = new UseHealSkillNode(agent);
		var supportSequence = new SequenceNode(new IBehaviourTreeNode[]{ isAllyNeedSupport, useSupportSkill });

		return supportSequence;
	}

	private IBehaviourTreeNode InitFollowTeamDirectionNode(ICharacterBehaviourTreeAgent agent)
	{
		var followTeamDirectionTargetNode = new FollowTeamDirectionTargetNode(agent);

		return followTeamDirectionTargetNode;
	}
}
}
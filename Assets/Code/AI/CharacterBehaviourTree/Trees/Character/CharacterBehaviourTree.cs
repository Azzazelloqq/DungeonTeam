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
		var attackEnemyNode = InitAttackEnemyNode(agent);

		var supportSequence = InitHealNode(agent);
		var followTeamDirectionNode = InitFollowTeamDirectionNode(agent);
		var returnToTeamNode = InitReturnToTeamNode(agent);
		
		_root = new SelectorNode(new[]{
			followTeamDirectionNode,
			supportSequence,
			attackEnemyNode,
			returnToTeamNode,
		});
	}

	public void Dispose()
	{
		_root.Dispose();
	}

	public void Tick()
	{
		_root.Tick();
	}

	private IBehaviourTreeNode InitAttackEnemyNode(ICharacterBehaviourTreeAgent agent)
	{
		var findEnemyInSightNode = new FindEnemyTargetInSightNode(agent);
		var trackEnemyInAttackRangeNode = new TrackEnemyInAttackRangeNode(agent);
		var moveToEnemyNode = new MoveToEnemyNode(agent);
		var attackEnemy = new AttackEnemyNode(agent);
		var useAttackSkillNode = InitUseAttackSkillNode(agent);

		var approachOrStaySelector = new SelectorNode(new IBehaviourTreeNode[]
		{
			trackEnemyInAttackRangeNode,
			moveToEnemyNode
		});

		var attackSelector = new SelectorNode(new[]
		{
			useAttackSkillNode,
			attackEnemy
		});

		var attackEnemySequence = new SequenceNode(new IBehaviourTreeNode[]
		{
			findEnemyInSightNode,
			approachOrStaySelector,
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
	
	private IBehaviourTreeNode InitReturnToTeamNode(ICharacterBehaviourTreeAgent agent)
	{
		var returnToTeamNode = new ReturnToTeamNode(agent);

		return returnToTeamNode;
	}
}
}
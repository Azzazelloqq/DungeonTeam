using System;
using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.AttackNodes;
using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.MoveNodes;
using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.SkillsNodes;
using Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.TrackNodes;
using Code.BehaviourTree;
using Code.BehaviourTree.Logger;
using Code.BehaviourTree.Nodes;

namespace Code.AI.CharacterBehaviourTree.Trees.Character
{
public class CharacterBehaviourTree : IBehaviourTree
{
	private readonly IBehaviourTreeNode _root;
	private readonly BehaviourTreeLogger _logger;

	public CharacterBehaviourTree(ICharacterBehaviourTreeAgent agent, Action<string> loggerAction, bool logNodes = false)
	{
		var attackEnemyNode = InitAttackEnemyNode(agent);

		var supportSequence = InitHealNode(agent);
		var followTeamDirectionNode = InitFollowTeamDirectionNode(agent);

		var returnToTeamNode = InitReturnToTeamNode(agent);

		_root = new SelectorNode(new[]
		{
			followTeamDirectionNode,
			supportSequence,
			attackEnemyNode,
			returnToTeamNode
		});

		if (logNodes)
		{
			var loggerSettings = new LoggerSettings($"[CharacterBehaviourTree] {agent.AgentName} | ", loggerAction);
			_logger = new BehaviourTreeLogger(loggerSettings);
			_root = _logger.WrapWithLogging(_root);
		}
	}

	public void Dispose()
	{
		_logger?.Dispose();
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
		var stopMovementNode = new StopMovementNode(agent);
		var moveToEnemySequence = new SequenceNode(new IBehaviourTreeNode[] { trackEnemyInAttackRangeNode, stopMovementNode });
		
		var approachOrStaySelector = new SelectorNode(new IBehaviourTreeNode[]
		{
			moveToEnemySequence,
			moveToEnemyNode
		});

		var attackEnemy = InitAttackNode(agent);
		var useAttackSkillNode = InitUseAttackSkillNode(agent);
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

		var useAttackSkillSequence = new SequenceNode(new IBehaviourTreeNode[]
		{
			isAvailableSkill,
			useSkill
		});

		return useAttackSkillSequence;
	}

	private IBehaviourTreeNode InitAttackNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAvailableAttackEnemyNode = new IsAvailableAttackEnemyNode(agent);
		var attackEnemy = new AttackEnemyNode(agent);

		var sequenceNode = new SequenceNode(new IBehaviourTreeNode[] { isAvailableAttackEnemyNode, attackEnemy });

		return sequenceNode;
	}

	private IBehaviourTreeNode InitHealNode(ICharacterBehaviourTreeAgent agent)
	{
		var isAllyNeedSupport = new FindTargetToHealNode(agent);
		var useSupportSkill = new UseHealSkillNode(agent);
		var supportSequence = new SequenceNode(new IBehaviourTreeNode[] { isAllyNeedSupport, useSupportSkill });

		return supportSequence;
	}

	private IBehaviourTreeNode InitFollowTeamDirectionNode(ICharacterBehaviourTreeAgent agent)
	{
		var followTeamDirectionTargetNode = new FollowTeamDirectionTargetNode(agent);

		var followTeamSequence = new SequenceNode(new IBehaviourTreeNode[]
			{ new IsAvailableMoveNode(agent), followTeamDirectionTargetNode });

		return followTeamSequence;
	}

	private IBehaviourTreeNode InitReturnToTeamNode(ICharacterBehaviourTreeAgent agent)
	{
		var returnToTeamNode = new ReturnToTeamNode(agent);

		var followTeamSequence = new SequenceNode(new IBehaviourTreeNode[]
			{ new IsAvailableMoveNode(agent), returnToTeamNode });

		return followTeamSequence;
	}
}
}
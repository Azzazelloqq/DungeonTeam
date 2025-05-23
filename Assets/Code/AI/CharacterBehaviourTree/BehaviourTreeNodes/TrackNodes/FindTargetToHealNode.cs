﻿using Code.AI.CharacterBehaviourTree.Agents;
using Code.AI.CharacterBehaviourTree.Agents.TrackAgents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes.TrackNodes
{
public class FindTargetToHealNode : IBehaviourTreeNode
{
	private readonly ITrackNeedSupportAgent _agent;

	public FindTargetToHealNode(ITrackNeedSupportAgent agent)
	{
		_agent = agent;
	}

	public NodeState Tick()
	{
		return _agent.TryFindTargetToHeal() ? NodeState.Success : NodeState.Failure;
	}

	public void Dispose()
	{
	}
}
}
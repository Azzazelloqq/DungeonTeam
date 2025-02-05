﻿using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class StayTreeNode : IBehaviourTreeNode
{
	private readonly IStayAgent _agent;

	public StayTreeNode(IStayAgent agent)
	{
		_agent = agent;
	}
	
	public NodeState Tick()
	{
		_agent.Stay();

		return NodeState.Running;
	}

	public void Dispose()
	{
	}
}
}
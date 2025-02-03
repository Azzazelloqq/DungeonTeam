﻿namespace Code.BehaviourTree.Nodes
{
public class SequenceNode : IBehaviourTreeNode
{
	private readonly IBehaviourTreeNode[] _children;

	public SequenceNode(IBehaviourTreeNode[] children)
	{
		_children = children;
	}
	
	public NodeState Tick()
	{
		foreach (var child in _children)
		{
			var state = child.Tick();
			if (state != NodeState.Success)
			{
				return state;
			}
		}
		
		return NodeState.Success;
	}

	public void Dispose()
	{
		foreach (var behaviourTreeNode in _children)
		{
			behaviourTreeNode.Dispose();
		}
	}
}
}
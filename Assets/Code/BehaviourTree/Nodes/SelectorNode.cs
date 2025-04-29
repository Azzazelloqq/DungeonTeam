using System.Collections.Generic;

namespace Code.BehaviourTree.Nodes
{
public class SelectorNode : ICompositeNode
{
	public IReadOnlyList<IReadOnlyBehaviourTreeNode> Children => _children;

	private readonly IBehaviourTreeNode[] _children;

	public SelectorNode(IBehaviourTreeNode[] children)
	{
		_children = children;
	}

	public NodeState Tick()
	{
		foreach (var child in _children)
		{
			var state = child.Tick();
			if (state != NodeState.Failure)
			{
				return state;
			}
		}

		return NodeState.Failure;
	}

	public void Dispose()
	{
		foreach (var child in _children)
		{
			child.Dispose();
		}
	}
}
}
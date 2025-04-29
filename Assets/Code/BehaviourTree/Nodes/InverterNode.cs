using System.Collections.Generic;

namespace Code.BehaviourTree.Nodes
{
public class InverterNode : ICompositeNode
{
	public IReadOnlyList<IReadOnlyBehaviourTreeNode> Children { get; }

	private readonly IBehaviourTreeNode _child;

	public InverterNode(IBehaviourTreeNode child)
	{
		_child = child;

		Children = new IReadOnlyBehaviourTreeNode[] { _child };
	}

	public NodeState Tick()
	{
		var state = _child.Tick();
		switch (state)
		{
			case NodeState.Success:
				return NodeState.Failure;
			case NodeState.Failure:
				return NodeState.Success;
			case NodeState.Running:
				return NodeState.Running;
		}

		return NodeState.Failure;
	}

	public void Dispose()
	{
		_child.Dispose();
	}
}
}
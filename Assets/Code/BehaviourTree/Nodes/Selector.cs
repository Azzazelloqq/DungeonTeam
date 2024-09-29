namespace Code.BehaviourTree.Nodes
{
public class Selector : IBehaviourTreeNode
{
	private readonly IBehaviourTreeNode[] _children;
	
	public Selector(IBehaviourTreeNode[] children)
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
}
}
namespace Code.BehaviourTree.Nodes
{
public class Sequence : IBehaviourTreeNode
{
	private readonly IBehaviourTreeNode[] _children;

	public Sequence(IBehaviourTreeNode[] children)
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
}
}
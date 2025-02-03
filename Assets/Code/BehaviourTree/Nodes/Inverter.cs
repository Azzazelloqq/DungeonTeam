namespace Code.BehaviourTree.Nodes
{
public class Inverter : IBehaviourTreeNode
{
	private readonly IBehaviourTreeNode _child;
	
	public Inverter(IBehaviourTreeNode child)
	{
		_child = child;
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
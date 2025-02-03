using System;

namespace Code.BehaviourTree
{
public interface IBehaviourTreeNode : IDisposable
{
	public NodeState Tick();
}
}
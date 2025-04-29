using System;

namespace Code.BehaviourTree
{
public interface IBehaviourTreeNode : IDisposable, IReadOnlyBehaviourTreeNode
{
	public NodeState Tick();
}
}
using System.Collections.Generic;

namespace Code.BehaviourTree
{
public interface ICompositeNode : IBehaviourTreeNode
{
	public IReadOnlyList<IReadOnlyBehaviourTreeNode> Children { get; }
}
}
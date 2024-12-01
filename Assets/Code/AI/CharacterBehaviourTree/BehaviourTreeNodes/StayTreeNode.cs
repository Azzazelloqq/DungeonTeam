using Code.AI.CharacterBehaviourTree.Agents;
using Code.BehaviourTree;

namespace Code.AI.CharacterBehaviourTree.BehaviourTreeNodes
{
public class StayTreeNode : IBehaviourTree
{
	private readonly IStayAgent _agent;

	public StayTreeNode(IStayAgent agent)
	{
		_agent = agent;
	}
	
	public void Tick()
	{
		_agent.Stay();
	}
}
}
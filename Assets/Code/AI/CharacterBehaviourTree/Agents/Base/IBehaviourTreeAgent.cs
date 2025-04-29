using System;

namespace Code.AI.CharacterBehaviourTree.Agents.Base
{
public interface IBehaviourTreeAgent : IDisposable
{
	public string AgentName { get; }
}
}
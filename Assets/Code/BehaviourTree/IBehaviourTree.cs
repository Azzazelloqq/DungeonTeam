using System;

namespace Code.BehaviourTree
{
public interface IBehaviourTree : IDisposable
{
	public void Tick();
}
}
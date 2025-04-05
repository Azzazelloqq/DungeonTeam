using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code.CompositionRoot
{
public interface ICompositionRoot : IDisposable
{
	public void Enter();
	public Task EnterAsync(CancellationToken token);
}
}
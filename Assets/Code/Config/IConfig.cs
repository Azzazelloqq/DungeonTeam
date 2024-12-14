using System.Threading;
using System.Threading.Tasks;

namespace Code.Config
{
public interface IConfig
{
	public bool IsInitialized { get; }
	
	public void Initialize();
	public Task InitializeAsync(CancellationToken token);
	public T GetConfigPage<T>() where T : IConfigPage;
}
}
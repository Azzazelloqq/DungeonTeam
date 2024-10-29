using System.Threading;
using System.Threading.Tasks;

namespace Code.Config
{
public interface IConfigParser
{
	public IConfigData[] Parse();
	public Task<IConfigData[]> ParseAsync(CancellationToken token);
}
}
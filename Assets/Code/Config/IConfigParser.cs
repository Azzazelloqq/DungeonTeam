using System.Threading;
using System.Threading.Tasks;

namespace Code.Config
{
public interface IConfigParser
{
	public IConfigPage[] Parse();
	public Task<IConfigPage[]> ParseAsync(CancellationToken token);
}
}
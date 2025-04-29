using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code.LevelsCore.LevelBase
{
public interface ILevel : IDisposable
{
	public Task LoadLevelAsync(CancellationToken token);
	public Task UnloadLevelAsync(CancellationToken token);

	public void LoadScene();
	public void UnloadScene();
}
}
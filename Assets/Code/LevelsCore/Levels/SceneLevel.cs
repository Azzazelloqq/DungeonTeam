using System.Threading;
using System.Threading.Tasks;
using Code.LevelsCore.LevelBase;
using Code.Utils.AsyncUtils;
using SceneSwitcher;

namespace Code.LevelsCore.Levels
{
public class SceneLevel : ILevel
{
	private readonly string _sceneId;
	private readonly ISceneSwitcher _sceneSwitcher;
	private readonly CancellationTokenSource _disposeTokenSource = new();

	public SceneLevel(string sceneId, ISceneSwitcher sceneSwitcher)
	{
		_sceneId = sceneId;
		_sceneSwitcher = sceneSwitcher;
	}
	
	public Task LoadLevelAsync(CancellationToken token)
	{
		_sceneSwitcher.SwitchToScene<>()
	}

	public Task UnloadLevelAsync(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public void LoadScene()
	{
	}

	public void UnloadScene()
	{
	}

	public void Dispose()
	{
		_disposeTokenSource.CancelAndDispose();
	}
}
}
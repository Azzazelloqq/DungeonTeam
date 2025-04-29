using Code.SavesContainers.TeamSave;
using LocalSaveSystem;
using LocalSaveSystem.Factory;

namespace Code.SavesContainers.Factory
{
public class SaveFactory : ISaveFactory
{
	public ISavable[] CreateSaves()
	{
		return new ISavable[]
		{
			new PlayerTeamSave()
		};
	}

	public void Dispose()
	{
	}
}
}
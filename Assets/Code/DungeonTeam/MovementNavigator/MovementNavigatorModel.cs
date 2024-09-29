using Code.DungeonTeam.MovementNavigator.Base;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorModel : MovementNavigatorModelBase
{
	public int CurrentCharactersCount { get; private set; }

	private readonly int _maxCharactersCount;

	public MovementNavigatorModel(int maxCharactersCount)
	{
		_maxCharactersCount = maxCharactersCount;
	}

	public override void UpdateCharactersCount(int charactersCount)
	{
		if (charactersCount > _maxCharactersCount) {
			CurrentCharactersCount = _maxCharactersCount;
			
			return;
		}

		CurrentCharactersCount = charactersCount;
	}
}
}
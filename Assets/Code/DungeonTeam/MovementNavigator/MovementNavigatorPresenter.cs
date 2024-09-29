using System.Collections.Generic;
using Code.DungeonTeam.MovementNavigator.Base;
using Code.DungeonTeam.TeamCharacter.Base;
using TickHandler;

namespace Code.DungeonTeam.MovementNavigator
{
public class MovementNavigatorPresenter : MovementNavigatorPresenterBase
{
	private readonly ITickHandler _tickHandler;
	private List<TeamCharacterPresenterBase> _characters;
	
	public MovementNavigatorPresenter(
		MovementNavigatorViewBase view,
		MovementNavigatorModelBase model,
		ITickHandler tickHandler) : base(view,
		model)
	{
		_tickHandler = tickHandler;
	}
	
	protected override void OnInitialize()
	{
		base.OnInitialize();
		
		model.Initialize();
		view.Initialize(this);
		
		InitializeCharacters();
	}

	public override void Dispose()
	{
		base.Dispose();
		
		model.Dispose();
		view.Dispose();
	}

	public void OnCharactersCountChanged(int charactersCount) {
		model.UpdateCharactersCount(charactersCount);
	}

	private void InitializeCharacters()
	{
	
	}
}
}
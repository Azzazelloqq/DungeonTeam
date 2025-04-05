using System;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.MoveController.Base
{
public abstract class MoveControllerPresenterBase : Presenter<MoveControllerViewBase, MoveControllerModelBase>, IMoveController
{
	public abstract event Action<Vector2> DirectionChanged; 
	public abstract event Action MoveStarted; 
	public abstract event Action MoveEnded; 
	
	protected MoveControllerPresenterBase(MoveControllerViewBase view, MoveControllerModelBase model) : base(view, model)
	{
	}

	public abstract Vector2 Direction { get; }

	internal abstract void OnPointerDown(Vector2 dragPosition);
	internal abstract void OnDrag(Vector2 dragPosition);
	internal abstract void OnPointerUp();
}
}
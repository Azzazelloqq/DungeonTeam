using System;
using Code.DungeonTeam.MoveController.Base;
using Code.ModelStructs;
using UnityEngine;

namespace Code.DungeonTeam.MoveController.VirtualJoystick
{
public class VirtualJoystickPresenter : MoveControllerPresenterBase
{
	public override event Action<Vector2> DirectionChanged;
	public override event Action MoveStarted;
	public override event Action MoveEnded;
	
	public VirtualJoystickPresenter(MoveControllerViewBase view, MoveControllerModelBase model) : base(view, model)
	{
	}

	protected override void OnDispose()
	{
		base.OnDispose();

		DirectionChanged = null;
		MoveStarted = null;
		MoveEnded = null;
	}

	internal override void OnPointerDown(Vector2 dragPosition)
	{
		OnDrag(dragPosition);
		MoveStarted?.Invoke();
	}

	internal override void OnDrag(Vector2 dragPosition)
	{
		var modelDragPosition = dragPosition.ToModelVector();
		model.OnDrag(modelDragPosition);
		
		var modelHandlePosition = model.HandlePosition.ToUnityVector();
		view.OnHandlePosition(modelHandlePosition);
		
		var modelDirection = model.Direction.ToUnityVector();
		DirectionChanged?.Invoke(modelDirection);
	}

	internal override void OnPointerUp()
	{
		model.OnPointerUp();
		
		var modelHandlePosition = model.HandlePosition.ToUnityVector();
		view.OnHandlePosition(modelHandlePosition);
		
		MoveEnded?.Invoke();
	}

}
}
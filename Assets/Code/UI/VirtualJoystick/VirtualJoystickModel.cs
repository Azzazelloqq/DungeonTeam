using System;
using Code.ModelStructs;
using Code.UI.VirtualJoystick.Base;

namespace Code.UI.VirtualJoystick
{
public class VirtualJoystickModel : VirtualJoystickModelBase
{
	public override event Action<Vector3> DirectionChanged;
	public override event Action<bool> InputStateChanged;

	public override Vector3 Direction { get; protected set; }
	public override bool IsActive { get; protected set; }

	public override void OnInputStarted()
	{
		InputStateChanged?.Invoke(true);
	}

	public override void OnInputEnded()
	{
		InputStateChanged?.Invoke(false);
	}

	public override void OnInputMoved(Vector2 inputPosition, Vector2 centerPosition)
	{
		var delta = inputPosition - centerPosition;
		var direction = new Vector3(delta.X, 0, delta.Y).Normalized;

		Direction = direction;
		DirectionChanged?.Invoke(Direction);
	}
}
}
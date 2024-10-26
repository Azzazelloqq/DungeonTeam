using System;
using MVVM.MVVM.System.Base.Model;
using Code.ModelStructs;

namespace Code.UI.VirtualJoystick.Base
{
public abstract class VirtualJoystickModelBase : ModelBase
{
	public abstract event Action<Vector3> DirectionChanged;
	public abstract event Action<bool> InputStateChanged;
	
	public abstract Vector3 Direction { get; protected set; }
	public abstract bool IsActive { get; protected set; }
	
	public abstract void OnInputStarted();
	public abstract void OnInputEnded();
	public abstract void OnInputMoved(Vector2 inputPosition, Vector2 centerPosition);
}
}
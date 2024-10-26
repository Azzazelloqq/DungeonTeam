using System;
using MVVM.MVVM.System.Base.ViewModel;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Code.UI.VirtualJoystick.Base
{
public abstract class VirtualJoystickViewModelBase : ViewModelBase<VirtualJoystickModelBase>
{
	public abstract event Action<Vector3> DirectionChanged;
	public abstract event Action<bool> InputStateChanged;
	
	public abstract Vector3 Direction { get; }
	public abstract bool IsActive { get; }
	
	public abstract void OnInputMoved(Vector2 inputPosition, Vector2 centerPosition);
	public abstract void OnInputStarted();
	public abstract void OnInputEnded();
	
	protected VirtualJoystickViewModelBase(VirtualJoystickModelBase model) : base(model)
	{
	}
}
}
using System;
using MVVM.MVVM.System.Base.Model;
using Code.ModelStructs;

namespace Code.UI.VirtualJoystick.Base
{
public abstract class VirtualJoystickModel : ModelBase
{
	public event Action<Vector3> OnDirectionChanged;
	public event Action<bool> OnActiveStateChanged;
	
	public Vector3 Direction { get; private set; }
	public bool IsActive { get; private set; }


	public void SetDirection(Vector3 direction)
	{
		Direction = direction;
		OnDirectionChanged?.Invoke(Direction);
	}

	public void SetActiveState(bool isActive)
	{
		IsActive = isActive;
		OnActiveStateChanged?.Invoke(IsActive);
	}
}
}
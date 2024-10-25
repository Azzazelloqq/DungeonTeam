using Code.ModelStructs;
using MVVM.MVVM.System.Base.ViewModel;
using Vector3 = UnityEngine.Vector3;

namespace Code.UI.VirtualJoystick.Base
{
public abstract class VirtualJoystickViewModelBase : ViewModelBase<VirtualJoystickModel>
{
	public event System.Action<Vector3> OnDirectionChanged;
	public event System.Action<bool> OnActiveStateChanged;
	
	public Vector3 Direction => model.Direction.ToUnityVector();

	public bool IsActive => model.IsActive;

	protected VirtualJoystickViewModelBase(VirtualJoystickModel model) : base(model)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
	}

	private void HandleDirectionChanged(Vector3 direction)
	{
		OnDirectionChanged?.Invoke(direction);
	}

	private void HandleActiveStateChanged(bool isActive)
	{
		OnActiveStateChanged?.Invoke(isActive);
	}

	public void OnInputMoved(UnityEngine.Vector2 inputPosition, UnityEngine.Vector2 centerPosition)
	{
		var delta = inputPosition - centerPosition;
		var direction = new UnityEngine.Vector3(delta.x, 0, delta.y).normalized;

		model.SetDirection(direction.ToModelVector());
	}

	public void OnInputStarted()
	{
		model.SetActiveState(true);
	}

	public void OnInputEnded()
	{
		model.SetActiveState(false);
		
		var zeroDirection = Code.ModelStructs.Vector3.Zero;
		model.SetDirection(zeroDirection);
	}
}
}
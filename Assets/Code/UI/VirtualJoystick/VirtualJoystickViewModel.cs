using System;
using Code.ModelStructs;
using Code.UI.VirtualJoystick.Base;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Code.UI.VirtualJoystick
{
public class VirtualJoystickViewModel : VirtualJoystickViewModelBase
{
	public override event Action<Vector3> DirectionChanged;
	public override event Action<bool> InputStateChanged;
	
	public override Vector3 Direction => model.Direction.ToUnityVector();
	public override bool IsActive => model.IsActive;


	public VirtualJoystickViewModel(VirtualJoystickModelBase model) : base(model)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();

		model.DirectionChanged += OnDirectionChanged;
		model.InputStateChanged += OnActiveChanged;
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		model.DirectionChanged -= OnDirectionChanged;
		model.InputStateChanged -= OnActiveChanged;
	}

	public override void OnInputMoved(Vector2 inputPosition, Vector2 centerPosition)
	{
		var centerModel = centerPosition.ToModelVector();
		var inputModel = inputPosition.ToModelVector();
		
		model.OnInputMoved(inputModel, centerModel);
	}

	public override void OnInputStarted()
	{
		model.OnInputStarted();
	}

	public override void OnInputEnded()
	{
		model.OnInputEnded();
	}
	private void OnActiveChanged(bool isActive)
	{
		InputStateChanged?.Invoke(isActive);
	}

	private void OnDirectionChanged(ModelStructs.Vector3 modelDirection)
	{
		var unityVector = modelDirection.ToUnityVector();
		DirectionChanged?.Invoke(unityVector);
	}
}
}
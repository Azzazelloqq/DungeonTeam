
using Code.DungeonTeam.MoveController.Base;
using Code.ModelStructs;

namespace Code.DungeonTeam.MoveController.VirtualJoystick
{
public class VirtualJoystickModel : MoveControllerModelBase
{
	public override ModelVector2 Direction { get; protected set; }
	public override ModelVector2 HandlePosition => Direction * _radius;

	private float _radius;

	public void SetRadius(float radius)
	{
		_radius = radius;
	}

	public override void OnDrag(ModelVector2 dragPosition)
	{
		var inputVector = dragPosition / _radius;
		inputVector = inputVector.Magnitude > 1.0f ? inputVector.Normalized : inputVector;
		
		Direction = inputVector;	
	}

	public override void OnPointerUp()
	{
		Direction = ModelVector2.Zero;
	}
}
}
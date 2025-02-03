
using Code.DungeonTeam.MoveController.Base;
using Code.Utils.ModelUtils;
using InGameLogger;

namespace Code.DungeonTeam.MoveController.VirtualJoystick
{
public class VirtualJoystickModel : MoveControllerModelBase
{
	private readonly IInGameLogger _logger;
	public override ModelVector2 Direction { get; protected set; }
	public override ModelVector2 HandlePosition => Direction * _radius;

	private readonly float _radius;

	public VirtualJoystickModel(IInGameLogger logger, float radius)
	{
		_logger = logger;
		_radius = radius;
	}
	
	public override void OnDrag(ModelVector2 dragPosition)
	{
		if (_radius <= 0)
		{
			_logger.LogError("Radius must be greater than zero");
			return;
		}
		
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
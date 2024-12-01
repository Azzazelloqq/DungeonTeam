using Code.ModelStructs;
using MVP;

namespace Code.DungeonTeam.MoveController.Base
{
public abstract class MoveControllerModelBase : Model
{
	public abstract ModelVector2 Direction { get; protected set; }
	public abstract ModelVector2 HandlePosition { get; }
	
	public abstract void OnDrag(ModelVector2 dragPosition);
	public abstract void OnPointerUp();
}
}
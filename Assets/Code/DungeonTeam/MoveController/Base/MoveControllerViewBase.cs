using MVP;
using UnityEngine;

namespace Code.DungeonTeam.MoveController.Base
{
public abstract class MoveControllerViewBase : ViewMonoBehaviour<MoveControllerPresenterBase>
{
	public abstract float ControllerHandleRadius { get; }
	public abstract void OnHandlePosition(Vector2 anchoredPosition);
}
}
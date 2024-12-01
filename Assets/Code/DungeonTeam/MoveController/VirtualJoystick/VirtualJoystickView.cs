using Code.DungeonTeam.MoveController.Base;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.DungeonTeam.MoveController.VirtualJoystick
{
public class VirtualJoystickView : MoveControllerViewBase, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField]
	private RectTransform _joystickBackground;

	[SerializeField]
	private RectTransform _joystickHandle;
	
	public override float ControllerHandleRadius => _joystickBackground.rect.width / 2;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_joystickBackground,
				eventData.position,
				eventData.pressEventCamera,
				out var localPoint))
		{
			return;
		}

		presenter.OnPointerDown(localPoint);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_joystickBackground,
				eventData.position,
				eventData.pressEventCamera,
				out var localPoint))
		{
			return;
		}
		
		presenter.OnDrag(localPoint);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		presenter.OnPointerUp();
	}

	public override void OnHandlePosition(Vector2 anchoredPosition)
	{
		_joystickHandle.anchoredPosition = anchoredPosition;
	}
}
}
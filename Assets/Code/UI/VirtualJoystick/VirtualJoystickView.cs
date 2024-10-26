using Code.UI.VirtualJoystick.Base;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.UI.VirtualJoystick
{
public class VirtualJoystickView : VirtualJoystickViewBase, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
	[SerializeField]
	private RectTransform _joystickBackground;

	[SerializeField]
	private RectTransform _joystickHandle;

	[SerializeField]
	private RectTransform _movementArea;
	
	private Vector2 _joystickCenter;
	private Vector2 _previousAreaSize;
	private float _joystickRadius;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		
		UpdateJoystickProperties();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		viewModel.OnInputStarted();
		OnDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		var inputPosition = eventData.position;

		var direction = inputPosition - _joystickCenter;
		if (direction.magnitude > _joystickRadius)
		{
			direction = direction.normalized * _joystickRadius;
		}

		_joystickHandle.position = _joystickCenter + direction;

		viewModel.OnInputMoved(inputPosition, _joystickCenter);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		viewModel.OnInputEnded();

		_joystickHandle.position = _joystickCenter;
	}
	
	private void UpdateJoystickProperties()
	{
		_joystickCenter = _movementArea.position;

		var width = _movementArea.rect.width;
		var height = _movementArea.rect.height;
		_joystickRadius = Mathf.Min(width, height) / 2f;

		_previousAreaSize = _movementArea.rect.size;
	}
}
}
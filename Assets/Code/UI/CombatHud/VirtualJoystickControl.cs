using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonTeam.UI.CombatHud
{
    internal sealed class VirtualJoystickControl : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        private const int NoPointer = int.MinValue;

        private RectTransform _area;
        private RectTransform _knob;
        private Action<Vector2> _movementChanged;
        private float _travelRadius;
        private int _activePointerId = NoPointer;

        public void Bind(
            RectTransform knob,
            float travelRadius,
            Action<Vector2> movementChanged)
        {
            if (knob == null)
                throw new ArgumentNullException(nameof(knob));
            if (travelRadius <= 0f || float.IsNaN(travelRadius) || float.IsInfinity(travelRadius))
                throw new ArgumentOutOfRangeException(nameof(travelRadius));
            if (movementChanged == null)
                throw new ArgumentNullException(nameof(movementChanged));

            Unbind();
            _area = (RectTransform)transform;
            _knob = knob;
            _travelRadius = travelRadius;
            _movementChanged = movementChanged;
            ResetPosition();
        }

        public void Unbind()
        {
            PublishZero();
            _movementChanged = null;
            _area = null;
            _knob = null;
            _travelRadius = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_movementChanged == null || _activePointerId != NoPointer)
                return;

            _activePointerId = eventData.pointerId;
            UpdatePosition(eventData);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_movementChanged == null || eventData.pointerId != _activePointerId)
                return;

            UpdatePosition(eventData);
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
                return;

            PublishZero();
            eventData.Use();
        }

        public void OnCancel(BaseEventData eventData)
        {
            PublishZero();
            eventData.Use();
        }

        private void OnDisable()
        {
            PublishZero();
        }

        private void UpdatePosition(PointerEventData eventData)
        {
            if (_area == null || _knob == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _area,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPosition))
            {
                PublishZero();
                return;
            }

            var clampedPosition = Vector2.ClampMagnitude(localPosition, _travelRadius);
            _knob.anchoredPosition = clampedPosition;
            _movementChanged(clampedPosition / _travelRadius);
        }

        private void PublishZero()
        {
            _activePointerId = NoPointer;
            ResetPosition();
            _movementChanged?.Invoke(Vector2.zero);
        }

        private void ResetPosition()
        {
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class PointerTargetSelectionInput : IDisposable
    {
        private readonly InputAction _targetSelection;
        private readonly List<RaycastResult> _uiRaycastResults = new();

        private Vector2 _pendingScreenPosition;
        private bool _hasPendingSelection;
        private bool _isDisposed;

        public PointerTargetSelectionInput(bool includeMouse, bool includeTouch)
        {
            if (!includeMouse && !includeTouch)
            {
                throw new ArgumentException("At least one pointer source is required.");
            }

            _targetSelection = new InputAction(
                "Hero Target Selection",
                InputActionType.PassThrough);
            if (includeMouse)
            {
                _targetSelection.AddBinding("<Mouse>/leftButton");
            }

            if (includeTouch)
            {
                _targetSelection.AddBinding("<Touchscreen>/touch*/press");
            }

            _targetSelection.performed += OnTargetSelectionPerformed;
        }

        public void Enable()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PointerTargetSelectionInput));
            }

            ClearPendingSelection();
            _targetSelection.Enable();
        }

        public bool TryConsume(out Vector2 screenPosition)
        {
            if (!_hasPendingSelection)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = _pendingScreenPosition;
            ClearPendingSelection();
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _targetSelection.performed -= OnTargetSelectionPerformed;
            _targetSelection.Disable();
            _targetSelection.Dispose();
            ClearPendingSelection();
            _uiRaycastResults.Clear();
        }

        private void OnTargetSelectionPerformed(InputAction.CallbackContext context)
        {
            if (context.control == null ||
                !context.control.IsPressed() ||
                !TryGetPointerPosition(context, out var screenPosition) ||
                IsPointerOverUi(screenPosition))
            {
                return;
            }

            _pendingScreenPosition = screenPosition;
            _hasPendingSelection = true;
        }

        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var eventData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };
            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(eventData, _uiRaycastResults);
            for (var index = 0; index < _uiRaycastResults.Count; index++)
            {
                if (_uiRaycastResults[index].module is GraphicRaycaster)
                {
                    _uiRaycastResults.Clear();
                    return true;
                }
            }

            _uiRaycastResults.Clear();
            return false;
        }

        private static bool TryGetPointerPosition(
            InputAction.CallbackContext context,
            out Vector2 screenPosition)
        {
            if (context.control.parent is TouchControl touch)
            {
                screenPosition = touch.position.ReadValue();
                return true;
            }

            if (context.control.device is Mouse mouse)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            screenPosition = Vector2.zero;
            return false;
        }

        private void ClearPendingSelection()
        {
            _pendingScreenPosition = Vector2.zero;
            _hasPendingSelection = false;
        }
    }
}

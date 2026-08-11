using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class DesktopDungeonRunInput : IDungeonRunInput
    {
        private readonly InputAction _movement;
        private readonly InputAction _cameraRotation;
        private readonly InputAction _cameraRotationEngaged;
        private readonly InputAction _targetSelection;
        private readonly InputAction _primarySkill;
        private readonly InputAction _active1Skill;
        private readonly List<RaycastResult> _uiRaycastResults = new();

        private Vector2 _pendingTargetSelection;
        private SkillSlot? _pendingSkillSlot;
        private bool _hasPendingTargetSelection;
        private bool _isDisposed;

        public DesktopDungeonRunInput()
        {
            _movement = new InputAction("Hero Movement", InputActionType.Value);
            _movement.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _cameraRotation = new InputAction(
                "Camera Rotation",
                InputActionType.PassThrough,
                "<Mouse>/delta");
            _cameraRotationEngaged = new InputAction(
                "Camera Rotation Engaged",
                InputActionType.Button,
                "<Mouse>/rightButton");
            _targetSelection = new InputAction(
                "Hero Target Selection",
                InputActionType.PassThrough);
            _targetSelection.AddBinding("<Mouse>/leftButton");
            _targetSelection.AddBinding("<Touchscreen>/touch*/press");
            _primarySkill = new InputAction(
                "Hero Primary Skill",
                InputActionType.Button,
                "<Keyboard>/space");
            _active1Skill = new InputAction(
                "Hero Active Skill 1",
                InputActionType.Button,
                "<Keyboard>/q");

            _targetSelection.performed += OnTargetSelectionPerformed;
            _primarySkill.performed += OnPrimarySkillPerformed;
            _active1Skill.performed += OnActive1SkillPerformed;
        }

        public Vector2 Movement => _isDisposed
            ? Vector2.zero
            : _movement.ReadValue<Vector2>();

        public float CameraYawDelta => !_isDisposed && _cameraRotationEngaged.IsPressed()
            ? _cameraRotation.ReadValue<Vector2>().x
            : 0f;

        public bool TryConsumeTargetSelection(out Vector2 screenPosition)
        {
            if (!_hasPendingTargetSelection)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = _pendingTargetSelection;
            _pendingTargetSelection = Vector2.zero;
            _hasPendingTargetSelection = false;
            return true;
        }

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            if (!_pendingSkillSlot.HasValue)
            {
                slot = default;
                return false;
            }

            slot = _pendingSkillSlot.Value;
            _pendingSkillSlot = null;
            return true;
        }

        public void Enable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DesktopDungeonRunInput));

            ClearPendingCommands();
            _movement.Enable();
            _cameraRotation.Enable();
            _cameraRotationEngaged.Enable();
            _targetSelection.Enable();
            _primarySkill.Enable();
            _active1Skill.Enable();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _targetSelection.performed -= OnTargetSelectionPerformed;
            _primarySkill.performed -= OnPrimarySkillPerformed;
            _active1Skill.performed -= OnActive1SkillPerformed;

            _movement.Disable();
            _cameraRotation.Disable();
            _cameraRotationEngaged.Disable();
            _targetSelection.Disable();
            _primarySkill.Disable();
            _active1Skill.Disable();

            _movement.Dispose();
            _cameraRotation.Dispose();
            _cameraRotationEngaged.Dispose();
            _targetSelection.Dispose();
            _primarySkill.Dispose();
            _active1Skill.Dispose();
            ClearPendingCommands();
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

            _pendingTargetSelection = screenPosition;
            _hasPendingTargetSelection = true;
        }

        private void OnPrimarySkillPerformed(InputAction.CallbackContext _)
        {
            _pendingSkillSlot = SkillSlot.Primary;
        }

        private void OnActive1SkillPerformed(InputAction.CallbackContext _)
        {
            _pendingSkillSlot = SkillSlot.Active1;
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

        private void ClearPendingCommands()
        {
            _pendingTargetSelection = Vector2.zero;
            _pendingSkillSlot = null;
            _hasPendingTargetSelection = false;
        }
    }
}

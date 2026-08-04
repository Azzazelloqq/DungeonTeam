using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class DesktopDungeonRunInput : IDungeonRunInput
    {
        private readonly InputAction _movement;
        private readonly InputAction _cameraRotation;
        private readonly InputAction _cameraRotationEngaged;
        private readonly InputAction _targetSelection;
        private readonly InputAction _basicAttack;

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
                InputActionType.Button,
                "<Mouse>/leftButton");
            _basicAttack = new InputAction(
                "Hero Basic Attack",
                InputActionType.Button,
                "<Keyboard>/space");
        }

        public Vector2 Movement => _movement.ReadValue<Vector2>();

        public float CameraYawDelta => _cameraRotationEngaged.IsPressed()
            ? _cameraRotation.ReadValue<Vector2>().x
            : 0f;

        public bool TargetSelectionWasPressed =>
            _targetSelection.WasPressedThisFrame() &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

        public Vector2 PointerPosition => Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        public bool BasicAttackWasPressed => _basicAttack.WasPressedThisFrame();

        public void Enable()
        {
            _movement.Enable();
            _cameraRotation.Enable();
            _cameraRotationEngaged.Enable();
            _targetSelection.Enable();
            _basicAttack.Enable();
        }

        public void Dispose()
        {
            _movement.Disable();
            _cameraRotation.Disable();
            _cameraRotationEngaged.Disable();
            _targetSelection.Disable();
            _basicAttack.Disable();

            _movement.Dispose();
            _cameraRotation.Dispose();
            _cameraRotationEngaged.Dispose();
            _targetSelection.Dispose();
            _basicAttack.Dispose();
        }
    }
}

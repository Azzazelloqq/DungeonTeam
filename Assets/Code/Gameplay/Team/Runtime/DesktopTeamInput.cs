using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    public sealed class DesktopTeamInput : ITeamInput
    {
        private readonly InputAction _movement;
        private readonly InputAction _cameraRotation;
        private readonly InputAction _cameraRotationEngaged;

        public DesktopTeamInput()
        {
            _movement = new InputAction("Team Movement", InputActionType.Value);
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
        }

        public Vector2 Movement => _movement.ReadValue<Vector2>();

        public float CameraYawDelta => _cameraRotationEngaged.IsPressed()
            ? _cameraRotation.ReadValue<Vector2>().x
            : 0f;

        public void Enable()
        {
            _movement.Enable();
            _cameraRotation.Enable();
            _cameraRotationEngaged.Enable();
        }

        public void Dispose()
        {
            _movement.Disable();
            _cameraRotation.Disable();
            _cameraRotationEngaged.Disable();

            _movement.Dispose();
            _cameraRotation.Dispose();
            _cameraRotationEngaged.Dispose();
        }
    }
}

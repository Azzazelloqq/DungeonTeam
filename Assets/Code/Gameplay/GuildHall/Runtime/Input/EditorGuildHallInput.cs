#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Input
{
    public sealed class EditorGuildHallInput : IGuildHallInput
    {
        private readonly InputAction _movement;
        private bool _isDisposed;

        public EditorGuildHallInput()
        {
            _movement = new InputAction("Guild Hall Movement", InputActionType.Value);
            _movement.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        public Vector2 Movement => _isDisposed
            ? Vector2.zero
            : _movement.ReadValue<Vector2>();

        public void Enable()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EditorGuildHallInput));
            }

            _movement.Enable();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _movement.Disable();
            _movement.Dispose();
        }
    }
}
#endif

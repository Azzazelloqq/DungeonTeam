#if UNITY_EDITOR
using System;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class EditorDungeonRunInput : IDungeonRunInput
    {
        private readonly InputAction _movement;
        private bool _isDisposed;

        public EditorDungeonRunInput()
        {
            _movement = new InputAction("Hero Movement", InputActionType.Value);
            _movement.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        public Vector2 Movement => _isDisposed
            ? Vector2.zero
            : _movement.ReadValue<Vector2>();

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            slot = default;
            return false;
        }

        public void Enable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(EditorDungeonRunInput));

            _movement.Enable();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _movement.Disable();
            _movement.Dispose();
        }
    }
}
#endif

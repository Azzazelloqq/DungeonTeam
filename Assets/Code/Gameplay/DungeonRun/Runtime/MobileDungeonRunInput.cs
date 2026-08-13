using System;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class MobileDungeonRunInput : IDungeonRunInput
    {
        private readonly PointerTargetSelectionInput _targetSelection = new(
            includeMouse: false,
            includeTouch: true);
        private bool _isDisposed;

        public Vector2 Movement => Vector2.zero;

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            slot = default;
            return false;
        }

        public bool TryConsumeTargetSelection(out Vector2 screenPosition)
        {
            return _targetSelection.TryConsume(out screenPosition);
        }

        public void Enable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MobileDungeonRunInput));

            _targetSelection.Enable();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _targetSelection.Dispose();
        }
    }
}

using System;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class MobileDungeonRunInput : IDungeonRunInput
    {
        private bool _isDisposed;

        public Vector2 Movement => Vector2.zero;

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            slot = default;
            return false;
        }

        public void Enable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MobileDungeonRunInput));
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}

using System;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class VirtualHeroInput : IHeroInput
    {
        private Vector2 _movement;
        private SkillSlot? _pendingSkillSlot;
        private bool _isEnabled;

        public Vector2 Movement => _isEnabled ? _movement : Vector2.zero;

        public void Enable()
        {
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
            _movement = Vector2.zero;
            _pendingSkillSlot = null;
        }

        public void SetMovement(Vector2 movement)
        {
            if (!_isEnabled)
            {
                return;
            }

            if (!IsFinite(movement.x) || !IsFinite(movement.y))
            {
                throw new ArgumentOutOfRangeException(nameof(movement));
            }

            _movement = Vector2.ClampMagnitude(movement, 1f);
        }

        public void RequestSkill(SkillSlot slot)
        {
            if (!_isEnabled)
            {
                return;
            }

            if (!Enum.IsDefined(typeof(SkillSlot), slot))
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            _pendingSkillSlot = slot;
        }

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            if (!_isEnabled || !_pendingSkillSlot.HasValue)
            {
                slot = default;
                return false;
            }

            slot = _pendingSkillSlot.Value;
            _pendingSkillSlot = null;
            return true;
        }

        public bool TryConsumeTargetSelection(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

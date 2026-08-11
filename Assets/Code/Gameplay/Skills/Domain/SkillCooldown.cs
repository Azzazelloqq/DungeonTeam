using System;

namespace DungeonTeam.Gameplay.Skills.Domain
{
    public sealed class SkillCooldown
    {
        private readonly float _duration;
        private float _remaining;

        public SkillCooldown(float duration)
        {
            _duration = duration > 0f
                ? duration
                : throw new ArgumentOutOfRangeException(nameof(duration));
        }

        public bool IsReady => _remaining <= 0f;
        public float Remaining => _remaining;

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _remaining = Math.Max(0f, _remaining - deltaTime);
        }

        public bool TryConsume()
        {
            if (!IsReady)
            {
                return false;
            }

            _remaining = _duration;
            return true;
        }
    }
}

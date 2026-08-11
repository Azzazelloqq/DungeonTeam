using System;

namespace DungeonTeam.Gameplay.Combat.Domain
{
    public sealed class AttackCooldown
    {
        private readonly float _duration;
        private float _remaining;

        public AttackCooldown(float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _duration = duration;
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

using System;

namespace DungeonTeam.Gameplay.Actors.Domain
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

        public bool Tick(float deltaTime, bool canAttack)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _remaining = Math.Max(0f, _remaining - deltaTime);
            if (!canAttack || _remaining > 0f)
            {
                return false;
            }

            _remaining = _duration;
            return true;
        }
    }
}

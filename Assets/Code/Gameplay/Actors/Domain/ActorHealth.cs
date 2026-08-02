using System;

namespace DungeonTeam.Gameplay.Actors.Domain
{
    public enum ActorDamageResult
    {
        Damaged,
        Killed,
        Ignored
    }

    public sealed class ActorHealth
    {
        public ActorHealth(int maximum)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            Maximum = maximum;
            Current = maximum;
        }

        public int Maximum { get; }

        public int Current { get; private set; }

        public bool IsAlive => Current > 0;

        public ActorDamageResult ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (!IsAlive)
            {
                return ActorDamageResult.Ignored;
            }

            Current = Math.Max(0, Current - amount);
            return Current == 0
                ? ActorDamageResult.Killed
                : ActorDamageResult.Damaged;
        }
    }
}

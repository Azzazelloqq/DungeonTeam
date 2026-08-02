using System;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    internal sealed class DungeonDeterministicRandom
    {
        private uint _state;

        public DungeonDeterministicRandom(int seed)
        {
            _state = seed == 0 ? 0x6D2B79F5u : unchecked((uint)seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (int)(_state % (uint)maxExclusive);
        }

        public int NextInclusive(int minInclusive, int maxInclusive)
        {
            return minInclusive == maxInclusive
                ? minInclusive
                : minInclusive + Next(maxInclusive - minInclusive + 1);
        }
    }
}

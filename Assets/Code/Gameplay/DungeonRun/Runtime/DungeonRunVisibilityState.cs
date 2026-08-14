using System;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunVisibilityState
    {
        private readonly bool[] _revealedZones;
        private readonly bool[] _openedDoors;
        private readonly int[] _doorZoneIndices;

        public DungeonRunVisibilityState(int zoneCount, int[] doorZoneIndices)
        {
            if (zoneCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(zoneCount));
            }

            if (doorZoneIndices == null)
            {
                throw new ArgumentNullException(nameof(doorZoneIndices));
            }

            _revealedZones = new bool[zoneCount];
            _openedDoors = new bool[doorZoneIndices.Length];
            _doorZoneIndices = new int[doorZoneIndices.Length];
            _revealedZones[0] = true;
            for (var index = 0; index < doorZoneIndices.Length; index++)
            {
                var zoneIndex = doorZoneIndices[index];
                if (zoneIndex <= 0 || zoneIndex >= zoneCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(doorZoneIndices));
                }

                _doorZoneIndices[index] = zoneIndex;
            }
        }

        public bool IsZoneRevealed(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= _revealedZones.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(zoneIndex));
            }

            return _revealedZones[zoneIndex];
        }

        public bool IsDoorOpened(int doorIndex)
        {
            if (doorIndex < 0 || doorIndex >= _openedDoors.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(doorIndex));
            }

            return _openedDoors[doorIndex];
        }

        public bool TryOpenDoor(int doorIndex, out int revealedZoneIndex)
        {
            if (doorIndex < 0 || doorIndex >= _openedDoors.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(doorIndex));
            }

            if (_openedDoors[doorIndex])
            {
                revealedZoneIndex = -1;
                return false;
            }

            _openedDoors[doorIndex] = true;
            revealedZoneIndex = _doorZoneIndices[doorIndex];
            _revealedZones[revealedZoneIndex] = true;
            return true;
        }
    }
}

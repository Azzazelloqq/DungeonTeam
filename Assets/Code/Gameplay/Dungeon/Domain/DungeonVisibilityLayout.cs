using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonVisibilityLayout
    {
        private readonly DungeonVisibilityDoor[] _doors;

        private DungeonVisibilityLayout()
        {
            _doors = Array.Empty<DungeonVisibilityDoor>();
        }

        public DungeonVisibilityLayout(int zoneCount, IReadOnlyList<DungeonVisibilityDoor> doors)
        {
            if (zoneCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(zoneCount));
            }

            if (doors == null || doors.Count == 0)
            {
                throw new ArgumentException("Visibility requires at least one door.", nameof(doors));
            }

            ZoneCount = zoneCount;
            _doors = new DungeonVisibilityDoor[doors.Count];
            for (var index = 0; index < doors.Count; index++)
            {
                var door = doors[index];
                if (door.RevealedZoneIndex <= 0 || door.RevealedZoneIndex >= zoneCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(doors),
                        "A door must reveal a non-entry zone in this visibility layout.");
                }

                _doors[index] = door;
            }
        }

        public static DungeonVisibilityLayout Empty => new DungeonVisibilityLayout();

        public bool HasAuthoredVisibility => _doors.Length != 0;

        public int ZoneCount { get; }

        public IReadOnlyList<DungeonVisibilityDoor> Doors => _doors;
    }

    public readonly struct DungeonVisibilityDoor
    {
        public DungeonVisibilityDoor(DungeonPose interactionPose, int revealedZoneIndex)
        {
            if (revealedZoneIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revealedZoneIndex));
            }

            InteractionPose = interactionPose;
            RevealedZoneIndex = revealedZoneIndex;
        }

        public DungeonPose InteractionPose { get; }

        public int RevealedZoneIndex { get; }
    }
}

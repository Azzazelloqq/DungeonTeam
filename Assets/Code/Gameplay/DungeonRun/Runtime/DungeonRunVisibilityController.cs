using System;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunVisibilityController
    {
        private readonly DungeonVisibilityLayout _layout;
        private readonly IDungeonVisibilityBinding _binding;
        private readonly DungeonRunVisibilityState _state;

        public DungeonRunVisibilityController(
            DungeonVisibilityLayout layout,
            IDungeonVisibilityBinding binding)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (!_layout.HasAuthoredVisibility)
            {
                return;
            }

            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
            var doorZoneIndices = new int[_layout.Doors.Count];
            for (var index = 0; index < doorZoneIndices.Length; index++)
            {
                doorZoneIndices[index] = _layout.Doors[index].RevealedZoneIndex;
            }

            _state = new DungeonRunVisibilityState(_layout.ZoneCount, doorZoneIndices);
            _binding.Initialize();
        }

        public bool IsEnabled => _state != null;

        public int DoorCount => _layout.Doors.Count;

        public bool IsZoneRevealed(int zoneIndex)
        {
            return _state == null || _state.IsZoneRevealed(zoneIndex);
        }

        public Vector3 GetDoorPosition(int doorIndex)
        {
            if (doorIndex < 0 || doorIndex >= _layout.Doors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(doorIndex));
            }

            return DungeonPoseConversion.ToPosition(_layout.Doors[doorIndex].InteractionPose);
        }

        public bool IsDoorClosed(int doorIndex)
        {
            return _state != null && !_state.IsDoorOpened(doorIndex);
        }

        public bool TryOpenDoor(int doorIndex)
        {
            if (_state == null || !_state.TryOpenDoor(doorIndex, out _))
            {
                return false;
            }

            _binding.RevealDoor(doorIndex);
            return true;
        }
    }
}

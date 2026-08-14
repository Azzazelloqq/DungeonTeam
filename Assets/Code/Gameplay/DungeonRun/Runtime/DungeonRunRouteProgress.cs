using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal enum DungeonRunRoutePhase
    {
        Entering = 0,
        Exploring = 1,
        Encounter = 2,
        Continuing = 3,
        Completed = 4
    }

    internal readonly struct DungeonRunRoutePoint
    {
        public DungeonRunRoutePoint(float x, float z)
        {
            if (!IsFinite(x))
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (!IsFinite(z))
            {
                throw new ArgumentOutOfRangeException(nameof(z));
            }

            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }

        internal float DistanceSquaredTo(DungeonRunRoutePoint other)
        {
            var xDistance = X - other.X;
            var zDistance = Z - other.Z;
            return xDistance * xDistance + zDistance * zDistance;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal sealed class DungeonRunRouteProgress
    {
        private readonly DungeonRunRoutePoint[] _checkpoints;
        private readonly int _encounterStartIndex;
        private readonly float _checkpointRadiusSquared;

        public DungeonRunRouteProgress(
            IReadOnlyList<DungeonRunRoutePoint> checkpoints,
            int encounterStartIndex,
            float checkpointRadius)
        {
            if (checkpoints == null)
            {
                throw new ArgumentNullException(nameof(checkpoints));
            }

            if (checkpoints.Count < 3)
            {
                throw new ArgumentException(
                    "A route requires entry, encounter, and exit checkpoints.",
                    nameof(checkpoints));
            }

            if (encounterStartIndex <= 0 || encounterStartIndex >= checkpoints.Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterStartIndex));
            }

            if (checkpointRadius <= 0f ||
                float.IsNaN(checkpointRadius) ||
                float.IsInfinity(checkpointRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(checkpointRadius));
            }

            _checkpoints = new DungeonRunRoutePoint[checkpoints.Count];
            for (var index = 0; index < checkpoints.Count; index++)
            {
                _checkpoints[index] = checkpoints[index];
            }

            _encounterStartIndex = encounterStartIndex;
            _checkpointRadiusSquared = checkpointRadius * checkpointRadius;
            Phase = DungeonRunRoutePhase.Entering;
        }

        public event Action<DungeonRunRoutePhase> PhaseChanged;

        public DungeonRunRoutePhase Phase { get; private set; }

        public int NextCheckpointIndex { get; private set; }

        public int CheckpointCount => _checkpoints.Length;

        public bool TryReachCheckpoint(
            int checkpointIndex,
            DungeonRunRoutePoint position)
        {
            if (Phase is DungeonRunRoutePhase.Encounter or DungeonRunRoutePhase.Completed ||
                checkpointIndex != NextCheckpointIndex)
            {
                return false;
            }

            var checkpoint = _checkpoints[checkpointIndex];
            if (checkpoint.DistanceSquaredTo(position) > _checkpointRadiusSquared)
            {
                return false;
            }

            NextCheckpointIndex++;
            if (checkpointIndex == 0)
            {
                SetPhase(DungeonRunRoutePhase.Exploring);
            }
            else if (checkpointIndex == _encounterStartIndex)
            {
                SetPhase(DungeonRunRoutePhase.Encounter);
            }
            else if (checkpointIndex == _checkpoints.Length - 1)
            {
                SetPhase(DungeonRunRoutePhase.Completed);
            }

            return true;
        }

        public bool CompleteEncounter()
        {
            if (Phase != DungeonRunRoutePhase.Encounter)
            {
                return false;
            }

            SetPhase(DungeonRunRoutePhase.Continuing);
            return true;
        }

        private void SetPhase(DungeonRunRoutePhase phase)
        {
            if (Phase == phase)
            {
                return;
            }

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}

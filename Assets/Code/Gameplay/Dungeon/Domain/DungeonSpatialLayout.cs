using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonSpatialLayout
    {
        private DungeonSpatialLayout()
        {
            RouteCheckpoints = Array.Empty<DungeonPose>();
            CameraShots = Array.Empty<DungeonCameraShot>();
            CompanionFormationOffsets = Array.Empty<DungeonVector3>();
            TacticalAnchors = Array.Empty<DungeonPose>();
        }

        public DungeonSpatialLayout(
            IReadOnlyList<DungeonPose> routeCheckpoints,
            IReadOnlyList<DungeonCameraShot> cameraShots,
            DungeonEncounterSpan encounter,
            IReadOnlyList<DungeonVector3> companionFormationOffsets,
            IReadOnlyList<DungeonPose> tacticalAnchors)
        {
            RouteCheckpoints = CopyRequired(
                routeCheckpoints,
                minimumCount: 2,
                nameof(routeCheckpoints));
            CameraShots = CopyRequired(cameraShots, minimumCount: 1, nameof(cameraShots));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            CompanionFormationOffsets = CopyRequired(
                companionFormationOffsets,
                minimumCount: 1,
                nameof(companionFormationOffsets));
            TacticalAnchors = CopyRequired(
                tacticalAnchors,
                minimumCount: 1,
                nameof(tacticalAnchors));

            ValidateRouteIndices();
        }

        public static DungeonSpatialLayout Empty => new DungeonSpatialLayout();

        public bool HasAuthoredData => RouteCheckpoints.Count != 0;
        public IReadOnlyList<DungeonPose> RouteCheckpoints { get; }
        public IReadOnlyList<DungeonCameraShot> CameraShots { get; }
        public DungeonEncounterSpan Encounter { get; }
        public IReadOnlyList<DungeonVector3> CompanionFormationOffsets { get; }
        public IReadOnlyList<DungeonPose> TacticalAnchors { get; }

        private void ValidateRouteIndices()
        {
            if (Encounter.StartCheckpointIndex >= RouteCheckpoints.Count ||
                Encounter.EndCheckpointIndex >= RouteCheckpoints.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Encounter),
                    "Encounter checkpoint indices must belong to the route.");
            }

            var previousCheckpointIndex = -1;
            for (var index = 0; index < CameraShots.Count; index++)
            {
                var checkpointIndex = CameraShots[index].RouteCheckpointIndex;
                if (checkpointIndex >= RouteCheckpoints.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(CameraShots),
                        "Camera shot checkpoint indices must belong to the route.");
                }

                if (checkpointIndex <= previousCheckpointIndex)
                {
                    throw new ArgumentException(
                        "Camera shots must follow strict route order.",
                        nameof(CameraShots));
                }

                previousCheckpointIndex = checkpointIndex;
            }
        }

        private static IReadOnlyList<T> CopyRequired<T>(
            IReadOnlyList<T> source,
            int minimumCount,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (source.Count < minimumCount)
            {
                throw new ArgumentException(
                    $"Collection must contain at least {minimumCount} item(s).",
                    parameterName);
            }

            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return Array.AsReadOnly(copy);
        }
    }

    public readonly struct DungeonCameraShot
    {
        public DungeonCameraShot(
            DungeonPose pose,
            int routeCheckpointIndex,
            float lookAheadDistance,
            float activationRange,
            float blendRange)
        {
            if (routeCheckpointIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(routeCheckpointIndex));
            }

            if (lookAheadDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lookAheadDistance));
            }

            if (activationRange <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(activationRange));
            }

            if (blendRange < 0f || blendRange > activationRange)
            {
                throw new ArgumentOutOfRangeException(nameof(blendRange));
            }

            Pose = pose;
            RouteCheckpointIndex = routeCheckpointIndex;
            LookAheadDistance = lookAheadDistance;
            ActivationRange = activationRange;
            BlendRange = blendRange;
        }

        public DungeonPose Pose { get; }
        public int RouteCheckpointIndex { get; }
        public float LookAheadDistance { get; }
        public float ActivationRange { get; }
        public float BlendRange { get; }
    }

    public sealed class DungeonEncounterSpan
    {
        public DungeonEncounterSpan(
            DungeonPose startPose,
            DungeonPose endPose,
            int startCheckpointIndex,
            int endCheckpointIndex)
        {
            if (startCheckpointIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startCheckpointIndex));
            }

            if (endCheckpointIndex <= startCheckpointIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endCheckpointIndex),
                    "Encounter end checkpoint must follow its start checkpoint.");
            }

            StartPose = startPose;
            EndPose = endPose;
            StartCheckpointIndex = startCheckpointIndex;
            EndCheckpointIndex = endCheckpointIndex;
        }

        public DungeonPose StartPose { get; }
        public DungeonPose EndPose { get; }
        public int StartCheckpointIndex { get; }
        public int EndCheckpointIndex { get; }
    }

    public readonly struct DungeonVector3
    {
        public DungeonVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonSpatialAuthoringReader
    {
        public static DungeonSpatialLayout Read(
            Transform mapRoot,
            DungeonMapAuthoring authoring)
        {
            if (!authoring.HasAnySpatialData)
            {
                return DungeonSpatialLayout.Empty;
            }

            var route = ReadRoute(mapRoot, authoring);
            var routeIndices = BuildRouteIndices(authoring.RouteCheckpoints);
            var cameraShots = ReadCameraShots(mapRoot, authoring.CameraShots, routeIndices);
            var encounter = ReadEncounter(mapRoot, authoring, routeIndices);
            var formationOffsets = ReadFormationOffsets(
                mapRoot,
                authoring.CompanionFormationAnchors);
            var tacticalAnchors = ReadPoses(
                mapRoot,
                authoring.TacticalAnchors,
                "tactical anchor");

            return new DungeonSpatialLayout(
                route,
                cameraShots,
                encounter,
                formationOffsets,
                tacticalAnchors);
        }

        private static DungeonPose[] ReadRoute(
            Transform mapRoot,
            DungeonMapAuthoring authoring)
        {
            var checkpoints = authoring.RouteCheckpoints;
            if (checkpoints == null || checkpoints.Length < 2)
            {
                throw new InvalidOperationException(
                    "Dungeon spatial route must contain at least entry and exit checkpoints.");
            }

            if (checkpoints[0] != authoring.Entry)
            {
                throw new InvalidOperationException(
                    "Dungeon spatial route must start at the entry marker.");
            }

            if (checkpoints[checkpoints.Length - 1] != authoring.Exit)
            {
                throw new InvalidOperationException(
                    "Dungeon spatial route must end at the exit marker.");
            }

            var seen = new HashSet<Transform>();
            var result = new DungeonPose[checkpoints.Length];
            for (var index = 0; index < checkpoints.Length; index++)
            {
                var checkpoint = RequireMarker(
                    mapRoot,
                    checkpoints[index],
                    $"route checkpoint at index {index}");
                if (!seen.Add(checkpoint))
                {
                    throw new InvalidOperationException(
                        $"Dungeon spatial route contains duplicate checkpoint at index {index}.");
                }

                if (index > 0 &&
                    (checkpoint.position - checkpoints[index - 1].position).sqrMagnitude <=
                    0.000001f)
                {
                    throw new InvalidOperationException(
                        $"Dungeon spatial route checkpoint at index {index} does not advance the route.");
                }

                result[index] = checkpoint.ToDungeonPose();
            }

            return result;
        }

        private static Dictionary<Transform, int> BuildRouteIndices(Transform[] route)
        {
            var result = new Dictionary<Transform, int>(route.Length);
            for (var index = 0; index < route.Length; index++)
            {
                result.Add(route[index], index);
            }

            return result;
        }

        private static DungeonCameraShot[] ReadCameraShots(
            Transform mapRoot,
            DungeonCameraShotAuthoring[] authoring,
            IReadOnlyDictionary<Transform, int> routeIndices)
        {
            if (authoring == null || authoring.Length == 0)
            {
                throw new InvalidOperationException(
                    "Dungeon spatial authoring requires at least one camera shot.");
            }

            var anchors = new HashSet<Transform>();
            var result = new DungeonCameraShot[authoring.Length];
            var previousCheckpointIndex = -1;
            for (var index = 0; index < authoring.Length; index++)
            {
                var shot = authoring[index];
                if (shot == null)
                {
                    throw new InvalidOperationException(
                        $"Dungeon camera shot at index {index} is missing.");
                }

                var anchor = RequireMarker(mapRoot, shot.Anchor, $"camera shot anchor at index {index}");
                if (!anchors.Add(anchor))
                {
                    throw new InvalidOperationException(
                        $"Dungeon spatial authoring contains duplicate camera shot anchor at index {index}.");
                }

                if (shot.RouteCheckpoint == null ||
                    !routeIndices.TryGetValue(shot.RouteCheckpoint, out var checkpointIndex))
                {
                    throw new InvalidOperationException(
                        $"Dungeon camera shot at index {index} must reference a route checkpoint.");
                }

                if (checkpointIndex <= previousCheckpointIndex)
                {
                    throw new InvalidOperationException(
                        "Dungeon camera shots must follow route order.");
                }

                if (shot.LookAheadDistance < 0f ||
                    shot.ActivationRange <= 0f ||
                    shot.BlendRange < 0f ||
                    shot.BlendRange > shot.ActivationRange)
                {
                    throw new InvalidOperationException(
                        $"Dungeon camera shot at index {index} has invalid blend data.");
                }

                result[index] = new DungeonCameraShot(
                    anchor.ToDungeonPose(),
                    checkpointIndex,
                    shot.LookAheadDistance,
                    shot.ActivationRange,
                    shot.BlendRange);
                previousCheckpointIndex = checkpointIndex;
            }

            return result;
        }

        private static DungeonEncounterSpan ReadEncounter(
            Transform mapRoot,
            DungeonMapAuthoring authoring,
            IReadOnlyDictionary<Transform, int> routeIndices)
        {
            var start = RequireMarker(mapRoot, authoring.EncounterStart, "encounter start");
            var end = RequireMarker(mapRoot, authoring.EncounterEnd, "encounter end");
            if (!routeIndices.TryGetValue(start, out var startIndex))
            {
                throw new InvalidOperationException(
                    "Dungeon encounter start must reference a route checkpoint.");
            }

            if (!routeIndices.TryGetValue(end, out var endIndex))
            {
                throw new InvalidOperationException(
                    "Dungeon encounter end must reference a route checkpoint.");
            }

            if (endIndex <= startIndex)
            {
                throw new InvalidOperationException(
                    "Dungeon encounter end must follow its start in route order.");
            }

            return new DungeonEncounterSpan(
                start.ToDungeonPose(),
                end.ToDungeonPose(),
                startIndex,
                endIndex);
        }

        private static DungeonVector3[] ReadFormationOffsets(
            Transform mapRoot,
            Transform[] anchors)
        {
            var markers = ReadMarkers(mapRoot, anchors, "companion formation anchor");
            var result = new DungeonVector3[markers.Length];
            for (var index = 0; index < markers.Length; index++)
            {
                var offset = mapRoot.InverseTransformPoint(markers[index].position);
                result[index] = new DungeonVector3(offset.x, offset.y, offset.z);
            }

            return result;
        }

        private static DungeonPose[] ReadPoses(
            Transform mapRoot,
            Transform[] anchors,
            string markerName)
        {
            var markers = ReadMarkers(mapRoot, anchors, markerName);
            var result = new DungeonPose[markers.Length];
            for (var index = 0; index < markers.Length; index++)
            {
                result[index] = markers[index].ToDungeonPose();
            }

            return result;
        }

        private static Transform[] ReadMarkers(
            Transform mapRoot,
            Transform[] markers,
            string markerName)
        {
            if (markers == null || markers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Dungeon spatial authoring requires at least one {markerName}.");
            }

            var seen = new HashSet<Transform>();
            for (var index = 0; index < markers.Length; index++)
            {
                var marker = RequireMarker(
                    mapRoot,
                    markers[index],
                    $"{markerName} at index {index}");
                if (!seen.Add(marker))
                {
                    throw new InvalidOperationException(
                        $"Dungeon spatial authoring contains duplicate {markerName} at index {index}.");
                }
            }

            return markers;
        }

        private static Transform RequireMarker(
            Transform mapRoot,
            Transform marker,
            string markerName)
        {
            if (marker == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon spatial {markerName} is missing.");
            }

            if (marker != mapRoot && !marker.IsChildOf(mapRoot))
            {
                throw new InvalidOperationException(
                    $"Dungeon spatial {markerName} must belong to the map hierarchy.");
            }

            return marker;
        }
    }
}

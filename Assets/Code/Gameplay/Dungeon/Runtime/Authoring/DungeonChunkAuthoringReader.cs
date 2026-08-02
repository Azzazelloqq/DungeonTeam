using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonChunkAuthoringReader
    {
        public static DungeonChunkMetadata ReadMetadata(GameObject chunkPrefab, string chunkId)
        {
            var authoring = RequireAuthoring(chunkPrefab);
            var boundsSize = authoring.BoundsSize;
            var boundsCenter = authoring.BoundsCenter;
            var portAuthorings = chunkPrefab.GetComponentsInChildren<DungeonConnectionPortAuthoring>(
                includeInactive: true);
            var ports = new DungeonChunkPort[portAuthorings.Length];

            for (var index = 0; index < portAuthorings.Length; index++)
            {
                var portAuthoring = portAuthorings[index];
                var localPosition = chunkPrefab.transform.InverseTransformPoint(
                    portAuthoring.transform.position);
                ports[index] = new DungeonChunkPort(
                    portAuthoring.PortId,
                    portAuthoring.PortType,
                    localPosition.x,
                    localPosition.z,
                    portAuthoring.Direction);
            }

            return new DungeonChunkMetadata(
                chunkId,
                new DungeonChunkBounds(
                    boundsCenter.x,
                    boundsCenter.z,
                    boundsSize.x,
                    boundsSize.z),
                ports);
        }

        public static DungeonPlacementData ReadPlacements(
            GameObject chunkInstance,
            string runtimeIdPrefix)
        {
            RequireAuthoring(chunkInstance);
            return DungeonPlacementReader.Read(chunkInstance, runtimeIdPrefix);
        }

        public static DungeonPose RequireEntryPose(GameObject chunkInstance)
        {
            var authoring = RequireAuthoring(chunkInstance);
            ValidateMarker(chunkInstance.transform, authoring.Entry, "entry");
            return authoring.Entry.ToDungeonPose();
        }

        public static DungeonPose RequireExitPose(GameObject chunkInstance)
        {
            var authoring = RequireAuthoring(chunkInstance);
            ValidateMarker(chunkInstance.transform, authoring.Exit, "exit");
            return authoring.Exit.ToDungeonPose();
        }

        private static DungeonChunkAuthoring RequireAuthoring(GameObject chunkRoot)
        {
            if (chunkRoot == null)
            {
                throw new ArgumentNullException(nameof(chunkRoot));
            }

            return chunkRoot.GetComponent<DungeonChunkAuthoring>() ??
                   throw new InvalidOperationException(
                       $"Chunk '{chunkRoot.name}' must have DungeonChunkAuthoring on its root.");
        }

        private static void ValidateMarker(
            Transform chunkRoot,
            Transform marker,
            string markerName)
        {
            if (marker == null)
            {
                throw new InvalidOperationException(
                    $"Chunk '{chunkRoot.name}' has no {markerName} marker.");
            }

            if (marker != chunkRoot && !marker.IsChildOf(chunkRoot))
            {
                throw new InvalidOperationException(
                    $"Chunk {markerName} marker must belong to its hierarchy.");
            }
        }
    }
}

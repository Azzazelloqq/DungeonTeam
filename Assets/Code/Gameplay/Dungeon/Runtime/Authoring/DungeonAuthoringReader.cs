using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonAuthoringReader
    {
        public static AuthoredDungeonMapData Read(GameObject mapRoot, string dungeonId, int seed)
        {
            if (mapRoot == null)
            {
                throw new ArgumentNullException(nameof(mapRoot));
            }

            var mapAuthoring = mapRoot.GetComponent<DungeonMapAuthoring>();
            if (mapAuthoring == null)
            {
                throw new InvalidOperationException(
                    $"Authored dungeon '{mapRoot.name}' must have DungeonMapAuthoring on its root.");
            }

            ValidateMarker(mapRoot.transform, mapAuthoring.Entry, "entry");
            ValidateMarker(mapRoot.transform, mapAuthoring.Exit, "exit");

            var spatialLayout = DungeonSpatialAuthoringReader.Read(
                mapRoot.transform,
                mapAuthoring);
            var visibilityLayout = DungeonVisibilityAuthoringReader.Read(
                mapRoot.transform,
                mapAuthoring.Visibility);
            var placements = DungeonPlacementReader.Read(mapRoot);

            return new AuthoredDungeonMapData(
                new DungeonMapSnapshot(
                    dungeonId,
                    seed,
                    mapAuthoring.Entry.ToDungeonPose(),
                    mapAuthoring.Exit.ToDungeonPose(),
                    spatialLayout,
                    visibilityLayout),
                placements.EnemyPlacements,
                placements.InterestPointPlacements,
                placements.ObjectivePlacements);
        }

        private static void ValidateMarker(Transform mapRoot, Transform marker, string markerName)
        {
            if (marker == null)
            {
                throw new InvalidOperationException(
                    $"Authored dungeon '{mapRoot.name}' has no {markerName} marker.");
            }

            if (marker != mapRoot && !marker.IsChildOf(mapRoot))
            {
                throw new InvalidOperationException(
                    $"Authored dungeon {markerName} marker must belong to the map hierarchy.");
            }
        }

    }

    internal sealed class AuthoredDungeonMapData
    {
        public AuthoredDungeonMapData(
            DungeonMapSnapshot snapshot,
            EnemyPlacement[] enemyPlacements,
            InterestPointPlacement[] interestPointPlacements,
            ObjectivePlacement[] objectivePlacements)
        {
            Snapshot = snapshot;
            EnemyPlacements = enemyPlacements;
            InterestPointPlacements = interestPointPlacements;
            ObjectivePlacements = objectivePlacements;
        }

        public DungeonMapSnapshot Snapshot { get; }
        public EnemyPlacement[] EnemyPlacements { get; }
        public InterestPointPlacement[] InterestPointPlacements { get; }
        public ObjectivePlacement[] ObjectivePlacements { get; }
    }
}

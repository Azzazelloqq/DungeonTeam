using System;
using System.Collections.Generic;
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

            var enemyPlacements = new List<EnemyPlacement>();
            var interestPointPlacements = new List<InterestPointPlacement>();
            var objectivePlacements = new List<ObjectivePlacement>();
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            var components = mapRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

            for (var index = 0; index < components.Length; index++)
            {
                switch (components[index])
                {
                    case EnemyPlacementAuthoring enemyAuthoring:
                        var enemy = enemyAuthoring.ToDomain();
                        AddUnique(placementIds, enemy.PlacementId);
                        enemyPlacements.Add(enemy);
                        break;

                    case InterestPointPlacementAuthoring interestPointAuthoring:
                        var interestPoint = interestPointAuthoring.ToDomain();
                        AddUnique(placementIds, interestPoint.PlacementId);
                        interestPointPlacements.Add(interestPoint);
                        break;

                    case ObjectivePlacementAuthoring objectiveAuthoring:
                        var objective = objectiveAuthoring.ToDomain();
                        AddUnique(placementIds, objective.PlacementId);
                        objectivePlacements.Add(objective);
                        break;
                }
            }

            return new AuthoredDungeonMapData(
                new DungeonMapSnapshot(
                    dungeonId,
                    seed,
                    mapAuthoring.Entry.ToDungeonPose(),
                    mapAuthoring.Exit.ToDungeonPose()),
                enemyPlacements.ToArray(),
                interestPointPlacements.ToArray(),
                objectivePlacements.ToArray());
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

        private static void AddUnique(ISet<string> placementIds, string placementId)
        {
            if (!placementIds.Add(placementId))
            {
                throw new InvalidOperationException(
                    $"Authored dungeon contains duplicate placement ID '{placementId}'.");
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

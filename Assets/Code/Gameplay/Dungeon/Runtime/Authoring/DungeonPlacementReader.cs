using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonPlacementReader
    {
        public static DungeonPlacementData Read(GameObject root, string runtimeIdPrefix = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var enemyPlacements = new List<EnemyPlacement>();
            var interestPointPlacements = new List<InterestPointPlacement>();
            var objectivePlacements = new List<ObjectivePlacement>();
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            var components = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

            for (var index = 0; index < components.Length; index++)
            {
                switch (components[index])
                {
                    case EnemyPlacementAuthoring enemyAuthoring:
                        var enemyPlacementId = Prefix(runtimeIdPrefix, enemyAuthoring.PlacementId);
                        var encounterGroupId = string.IsNullOrWhiteSpace(enemyAuthoring.EncounterGroupId)
                            ? enemyAuthoring.EncounterGroupId
                            : Prefix(runtimeIdPrefix, enemyAuthoring.EncounterGroupId);
                        AddUnique(placementIds, enemyPlacementId);
                        enemyPlacements.Add(enemyAuthoring.ToDomain(
                            enemyPlacementId,
                            encounterGroupId));
                        break;

                    case InterestPointPlacementAuthoring interestPointAuthoring:
                        var interestPointPlacementId = Prefix(
                            runtimeIdPrefix,
                            interestPointAuthoring.PlacementId);
                        AddUnique(placementIds, interestPointPlacementId);
                        interestPointPlacements.Add(
                            interestPointAuthoring.ToDomain(interestPointPlacementId));
                        break;

                    case ObjectivePlacementAuthoring objectiveAuthoring:
                        var objectivePlacementId = Prefix(
                            runtimeIdPrefix,
                            objectiveAuthoring.PlacementId);
                        AddUnique(placementIds, objectivePlacementId);
                        objectivePlacements.Add(objectiveAuthoring.ToDomain(objectivePlacementId));
                        break;
                }
            }

            return new DungeonPlacementData(
                enemyPlacements.ToArray(),
                interestPointPlacements.ToArray(),
                objectivePlacements.ToArray());
        }

        private static string Prefix(string prefix, string value)
        {
            return string.IsNullOrEmpty(prefix) ? value : $"{prefix}.{value}";
        }

        private static void AddUnique(ISet<string> placementIds, string placementId)
        {
            if (!placementIds.Add(placementId))
            {
                throw new InvalidOperationException(
                    $"Dungeon authoring contains duplicate placement ID '{placementId}'.");
            }
        }
    }

    internal sealed class DungeonPlacementData
    {
        public DungeonPlacementData(
            EnemyPlacement[] enemyPlacements,
            InterestPointPlacement[] interestPointPlacements,
            ObjectivePlacement[] objectivePlacements)
        {
            EnemyPlacements = enemyPlacements;
            InterestPointPlacements = interestPointPlacements;
            ObjectivePlacements = objectivePlacements;
        }

        public EnemyPlacement[] EnemyPlacements { get; }
        public InterestPointPlacement[] InterestPointPlacements { get; }
        public ObjectivePlacement[] ObjectivePlacements { get; }
    }
}

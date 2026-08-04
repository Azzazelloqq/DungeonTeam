using System;
using System.Collections.Generic;
using System.Threading;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal static class DungeonProceduralMapAssembler
    {
        private const string EnemySlotTag = "enemy.common";
        private const string InterestPointSlotTag = "interest.common";
        private const string ObjectiveSlotTag = "objective.exit";

        [NoAutoStaticsCleanup]
        private static readonly CellSide[] CellSides =
        {
            new CellSide(0, 1, 0f, "North"),
            new CellSide(1, 0, 90f, "East"),
            new CellSide(0, -1, 180f, "South"),
            new CellSide(-1, 0, 270f, "West")
        };

        public static DungeonProceduralMapData Build(
            GameObject mapRoot,
            string dungeonId,
            int seed,
            DungeonProceduralLayout layout,
            DungeonTileSetAsset tileSet,
            CancellationToken token)
        {
            if (mapRoot == null)
            {
                throw new ArgumentNullException(nameof(mapRoot));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (tileSet == null)
            {
                throw new ArgumentNullException(nameof(tileSet));
            }

            tileSet.Validate();
            var cellIndices = BuildCellIndices(layout);
            var enemies = new List<EnemyPlacement>(layout.Cells.Count);
            var interestPoints = new List<InterestPointPlacement>(layout.Cells.Count);
            var objectives = new List<ObjectivePlacement>(1);

            for (var index = 0; index < layout.Cells.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var cell = layout.Cells[index];
                var center = CellCenter(cell, tileSet.CellSize);
                var floor = Object.Instantiate(
                    tileSet.FloorPrefab,
                    center,
                    Quaternion.identity,
                    mapRoot.transform);
                floor.name = $"Floor_{cell.GridX}_{cell.GridZ}";

                AddBoundaries(mapRoot.transform, layout, cellIndices, index, center, tileSet);
                AddPlacements(cell, center, tileSet, enemies, interestPoints, objectives);
            }

            var entryCenter = CellCenter(
                layout.Cells[layout.EntryCellIndex],
                tileSet.CellSize);
            var exitCenter = CellCenter(
                layout.Cells[layout.ExitCellIndex],
                tileSet.CellSize);
            return new DungeonProceduralMapData(
                new DungeonMapSnapshot(
                    dungeonId,
                    seed,
                    ToPose(entryCenter),
                    ToPose(exitCenter)),
                enemies.ToArray(),
                interestPoints.ToArray(),
                objectives.ToArray());
        }

        private static Dictionary<long, int> BuildCellIndices(DungeonProceduralLayout layout)
        {
            var indices = new Dictionary<long, int>(layout.Cells.Count);
            for (var index = 0; index < layout.Cells.Count; index++)
            {
                var cell = layout.Cells[index];
                indices.Add(CoordinateKey(cell.GridX, cell.GridZ), index);
            }

            return indices;
        }

        private static void AddBoundaries(
            Transform parent,
            DungeonProceduralLayout layout,
            IReadOnlyDictionary<long, int> cellIndices,
            int cellIndex,
            Vector3 center,
            DungeonTileSetAsset tileSet)
        {
            var cell = layout.Cells[cellIndex];
            for (var sideIndex = 0; sideIndex < CellSides.Length; sideIndex++)
            {
                var side = CellSides[sideIndex];
                var neighbourKey = CoordinateKey(
                    cell.GridX + side.GridX,
                    cell.GridZ + side.GridZ);
                var hasNeighbour = cellIndices.TryGetValue(neighbourKey, out var neighbourIndex);
                if (hasNeighbour && cellIndex > neighbourIndex)
                {
                    continue;
                }

                var hasPassage = hasNeighbour && layout.HasConnection(cellIndex, neighbourIndex);
                var prefab = hasPassage ? tileSet.PassagePrefab : tileSet.WallPrefab;
                var position = center + new Vector3(
                    side.GridX * tileSet.CellSize * 0.5f,
                    0f,
                    side.GridZ * tileSet.CellSize * 0.5f);
                var boundary = Object.Instantiate(
                    prefab,
                    position,
                    Quaternion.Euler(0f, side.Yaw, 0f),
                    parent);
                boundary.name = $"{(hasPassage ? "Passage" : "Wall")}_" +
                                $"{cell.GridX}_{cell.GridZ}_{side.Name}";
            }
        }

        private static void AddPlacements(
            DungeonProceduralCell cell,
            Vector3 center,
            DungeonTileSetAsset tileSet,
            ICollection<EnemyPlacement> enemies,
            ICollection<InterestPointPlacement> interestPoints,
            ICollection<ObjectivePlacement> objectives)
        {
            var idPrefix = $"procedural.{cell.GridX}.{cell.GridZ}";
            if ((cell.Slots & DungeonProceduralCellSlots.Enemy) != 0)
            {
                enemies.Add(new EnemyPlacement(
                    $"{idPrefix}.enemy",
                    DungeonPlacementMode.Slot,
                    EnemySlotTag,
                    fixedEnemyId: null,
                    fixedBehaviorId: null,
                    encounterGroupId: null,
                    ToPose(center + tileSet.EnemySlotOffset)));
            }

            if ((cell.Slots & DungeonProceduralCellSlots.InterestPoint) != 0)
            {
                interestPoints.Add(new InterestPointPlacement(
                    $"{idPrefix}.interest",
                    DungeonPlacementMode.Slot,
                    InterestPointSlotTag,
                    fixedInterestPointId: null,
                    fixedRewardProfileId: null,
                    ToPose(center + tileSet.InterestPointSlotOffset)));
            }

            if ((cell.Slots & DungeonProceduralCellSlots.Objective) != 0)
            {
                objectives.Add(new ObjectivePlacement(
                    $"{idPrefix}.objective",
                    ObjectiveSlotTag,
                    ToPose(center + tileSet.ObjectiveSlotOffset)));
            }
        }

        private static Vector3 CellCenter(DungeonProceduralCell cell, float cellSize)
        {
            return new Vector3(cell.GridX * cellSize, 0f, cell.GridZ * cellSize);
        }

        private static DungeonPose ToPose(Vector3 position)
        {
            return new DungeonPose(
                position.x,
                position.y,
                position.z,
                0f,
                0f,
                0f,
                1f);
        }

        private static long CoordinateKey(int gridX, int gridZ)
        {
            return ((long)gridX << 32) ^ (uint)gridZ;
        }

        private readonly struct CellSide
        {
            public CellSide(int gridX, int gridZ, float yaw, string name)
            {
                GridX = gridX;
                GridZ = gridZ;
                Yaw = yaw;
                Name = name;
            }

            public int GridX { get; }
            public int GridZ { get; }
            public float Yaw { get; }
            public string Name { get; }
        }
    }

    internal sealed class DungeonProceduralMapData
    {
        public DungeonProceduralMapData(
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

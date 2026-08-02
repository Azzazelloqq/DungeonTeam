using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonProceduralLayoutPlanner
    {
        private static readonly GridOffset[] CardinalOffsets =
        {
            new GridOffset(0, 1),
            new GridOffset(1, 0),
            new GridOffset(0, -1),
            new GridOffset(-1, 0)
        };

        public DungeonProceduralLayout Build(
            int seed,
            int width,
            int height,
            int targetCellCount,
            int mainRouteCellCount,
            int maxGenerationAttempts)
        {
            ValidateArguments(
                width,
                height,
                targetCellCount,
                mainRouteCellCount,
                maxGenerationAttempts);

            for (var attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                var random = new DungeonDeterministicRandom(
                    unchecked(seed + attempt * 486187739));
                if (TryBuild(
                        width,
                        height,
                        targetCellCount,
                        mainRouteCellCount,
                        random,
                        out var layout))
                {
                    return layout;
                }
            }

            throw new DungeonLayoutGenerationException(
                $"Could not build a {targetCellCount}-cell procedural layout after " +
                $"{maxGenerationAttempts} attempts.");
        }

        private static bool TryBuild(
            int width,
            int height,
            int targetCellCount,
            int mainRouteCellCount,
            DungeonDeterministicRandom random,
            out DungeonProceduralLayout layout)
        {
            var cells = new List<CellDraft>(targetCellCount)
            {
                new CellDraft(0, 0, -1, isMainRoute: true)
            };
            var occupied = new HashSet<long> { CoordinateKey(0, 0) };

            while (cells.Count < mainRouteCellCount)
            {
                if (!TryAddMainRouteCell(cells, occupied, width, height, random))
                {
                    layout = null;
                    return false;
                }
            }

            var exitCellIndex = cells.Count - 1;
            while (cells.Count < targetCellCount)
            {
                if (!TryAddBranchCell(
                        cells,
                        occupied,
                        width,
                        height,
                        exitCellIndex,
                        random))
                {
                    layout = null;
                    return false;
                }
            }

            var childCounts = new int[cells.Count];
            for (var index = 1; index < cells.Count; index++)
            {
                childCounts[cells[index].ParentCellIndex]++;
            }

            var result = new DungeonProceduralCell[cells.Count];
            for (var index = 0; index < cells.Count; index++)
            {
                var draft = cells[index];
                var slots = DungeonProceduralCellSlots.None;
                if (index != 0 && index != exitCellIndex)
                {
                    slots |= DungeonProceduralCellSlots.Enemy;
                }

                if (!draft.IsMainRoute && childCounts[index] == 0)
                {
                    slots |= DungeonProceduralCellSlots.InterestPoint;
                }

                if (index == exitCellIndex)
                {
                    slots |= DungeonProceduralCellSlots.Objective;
                }

                result[index] = new DungeonProceduralCell(
                    draft.GridX,
                    draft.GridZ,
                    draft.ParentCellIndex,
                    draft.IsMainRoute,
                    slots);
            }

            layout = new DungeonProceduralLayout(width, height, exitCellIndex, result);
            return true;
        }

        private static bool TryAddMainRouteCell(
            IList<CellDraft> cells,
            ISet<long> occupied,
            int width,
            int height,
            DungeonDeterministicRandom random)
        {
            var parentIndex = cells.Count - 1;
            var parent = cells[parentIndex];
            var directionStart = random.Next(CardinalOffsets.Length);
            for (var offset = 0; offset < CardinalOffsets.Length; offset++)
            {
                var direction = CardinalOffsets[(directionStart + offset) % CardinalOffsets.Length];
                var gridX = parent.GridX + direction.X;
                var gridZ = parent.GridZ + direction.Z;
                if (!CanOccupy(gridX, gridZ, width, height, occupied))
                {
                    continue;
                }

                cells.Add(new CellDraft(gridX, gridZ, parentIndex, isMainRoute: true));
                occupied.Add(CoordinateKey(gridX, gridZ));
                return true;
            }

            return false;
        }

        private static bool TryAddBranchCell(
            IList<CellDraft> cells,
            ISet<long> occupied,
            int width,
            int height,
            int exitCellIndex,
            DungeonDeterministicRandom random)
        {
            var parentStart = random.Next(cells.Count);
            var directionStart = random.Next(CardinalOffsets.Length);
            for (var parentOffset = 0; parentOffset < cells.Count; parentOffset++)
            {
                var parentIndex = (parentStart + parentOffset) % cells.Count;
                if (parentIndex == exitCellIndex)
                {
                    continue;
                }

                var parent = cells[parentIndex];
                for (var directionOffset = 0;
                     directionOffset < CardinalOffsets.Length;
                     directionOffset++)
                {
                    var direction = CardinalOffsets[
                        (directionStart + directionOffset) % CardinalOffsets.Length];
                    var gridX = parent.GridX + direction.X;
                    var gridZ = parent.GridZ + direction.Z;
                    if (!CanOccupy(gridX, gridZ, width, height, occupied))
                    {
                        continue;
                    }

                    cells.Add(new CellDraft(gridX, gridZ, parentIndex, isMainRoute: false));
                    occupied.Add(CoordinateKey(gridX, gridZ));
                    return true;
                }
            }

            return false;
        }

        private static bool CanOccupy(
            int gridX,
            int gridZ,
            int width,
            int height,
            ISet<long> occupied)
        {
            return gridX >= 0 && gridX < width &&
                   gridZ >= 0 && gridZ < height &&
                   !occupied.Contains(CoordinateKey(gridX, gridZ));
        }

        private static long CoordinateKey(int gridX, int gridZ)
        {
            return ((long)gridX << 32) ^ (uint)gridZ;
        }

        private static void ValidateArguments(
            int width,
            int height,
            int targetCellCount,
            int mainRouteCellCount,
            int maxGenerationAttempts)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            var capacity = (long)width * height;
            if (targetCellCount <= 2 || targetCellCount > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCellCount));
            }

            if (mainRouteCellCount < 2 || mainRouteCellCount >= targetCellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(mainRouteCellCount));
            }

            if (maxGenerationAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGenerationAttempts));
            }
        }

        private readonly struct CellDraft
        {
            public CellDraft(int gridX, int gridZ, int parentCellIndex, bool isMainRoute)
            {
                GridX = gridX;
                GridZ = gridZ;
                ParentCellIndex = parentCellIndex;
                IsMainRoute = isMainRoute;
            }

            public int GridX { get; }
            public int GridZ { get; }
            public int ParentCellIndex { get; }
            public bool IsMainRoute { get; }
        }

        private readonly struct GridOffset
        {
            public GridOffset(int x, int z)
            {
                X = x;
                Z = z;
            }

            public int X { get; }
            public int Z { get; }
        }
    }
}

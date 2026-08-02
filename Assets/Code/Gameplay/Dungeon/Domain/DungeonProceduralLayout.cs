using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    [Flags]
    public enum DungeonProceduralCellSlots
    {
        None = 0,
        Enemy = 1,
        InterestPoint = 2,
        Objective = 4
    }

    public readonly struct DungeonProceduralCell
    {
        public DungeonProceduralCell(
            int gridX,
            int gridZ,
            int parentCellIndex,
            bool isMainRoute,
            DungeonProceduralCellSlots slots)
        {
            GridX = gridX;
            GridZ = gridZ;
            ParentCellIndex = parentCellIndex;
            IsMainRoute = isMainRoute;
            Slots = slots;
        }

        public int GridX { get; }
        public int GridZ { get; }
        public int ParentCellIndex { get; }
        public bool IsMainRoute { get; }
        public DungeonProceduralCellSlots Slots { get; }
    }

    public sealed class DungeonProceduralLayout
    {
        internal DungeonProceduralLayout(
            int width,
            int height,
            int exitCellIndex,
            DungeonProceduralCell[] cells)
        {
            Width = width;
            Height = height;
            ExitCellIndex = exitCellIndex;
            Cells = Array.AsReadOnly((DungeonProceduralCell[])cells.Clone());
        }

        public int Width { get; }
        public int Height { get; }
        public int EntryCellIndex => 0;
        public int ExitCellIndex { get; }
        public IReadOnlyList<DungeonProceduralCell> Cells { get; }

        public bool HasConnection(int firstCellIndex, int secondCellIndex)
        {
            ValidateCellIndex(firstCellIndex, nameof(firstCellIndex));
            ValidateCellIndex(secondCellIndex, nameof(secondCellIndex));
            return Cells[firstCellIndex].ParentCellIndex == secondCellIndex ||
                   Cells[secondCellIndex].ParentCellIndex == firstCellIndex;
        }

        private void ValidateCellIndex(int cellIndex, string parameterName)
        {
            if (cellIndex < 0 || cellIndex >= Cells.Count)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}

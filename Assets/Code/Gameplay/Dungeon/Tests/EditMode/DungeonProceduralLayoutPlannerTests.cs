using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Dungeon.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonProceduralLayoutPlannerTests
    {
        [Test]
        public void Build_WithSameSeed_ReturnsSameLayout()
        {
            var planner = new DungeonProceduralLayoutPlanner();

            var first = planner.Build(42, 8, 8, 10, 7, 8);
            var second = planner.Build(42, 8, 8, 10, 7, 8);

            Assert.That(second.ExitCellIndex, Is.EqualTo(first.ExitCellIndex));
            Assert.That(second.Cells.Count, Is.EqualTo(first.Cells.Count));
            for (var index = 0; index < first.Cells.Count; index++)
            {
                AssertCell(second.Cells[index], first.Cells[index]);
            }
        }

        [Test]
        public void Build_WithValidDefinition_ReturnsRequestedTreeAndSemanticSlots()
        {
            const int width = 8;
            const int height = 8;
            const int targetCellCount = 10;
            const int mainRouteCellCount = 7;
            var planner = new DungeonProceduralLayoutPlanner();

            var layout = planner.Build(
                42,
                width,
                height,
                targetCellCount,
                mainRouteCellCount,
                8);

            Assert.That(layout.Cells.Count, Is.EqualTo(targetCellCount));
            Assert.That(layout.EntryCellIndex, Is.Zero);
            Assert.That(layout.ExitCellIndex, Is.EqualTo(mainRouteCellCount - 1));

            var occupied = new HashSet<long>();
            var childCounts = new int[targetCellCount];
            for (var index = 0; index < layout.Cells.Count; index++)
            {
                var cell = layout.Cells[index];
                Assert.That(cell.GridX, Is.InRange(0, width - 1));
                Assert.That(cell.GridZ, Is.InRange(0, height - 1));
                Assert.That(occupied.Add(CoordinateKey(cell.GridX, cell.GridZ)), Is.True);
                Assert.That(cell.IsMainRoute, Is.EqualTo(index < mainRouteCellCount));

                if (index == layout.EntryCellIndex)
                {
                    Assert.That(cell.ParentCellIndex, Is.EqualTo(-1));
                }
                else
                {
                    Assert.That(cell.ParentCellIndex, Is.InRange(0, index - 1));
                    childCounts[cell.ParentCellIndex]++;
                }
            }

            Assert.That(childCounts[layout.ExitCellIndex], Is.Zero);
            for (var index = 0; index < layout.Cells.Count; index++)
            {
                var cell = layout.Cells[index];
                var expectedSlots = DungeonProceduralCellSlots.None;
                if (index != layout.EntryCellIndex && index != layout.ExitCellIndex)
                {
                    expectedSlots |= DungeonProceduralCellSlots.Enemy;
                }

                if (!cell.IsMainRoute && childCounts[index] == 0)
                {
                    expectedSlots |= DungeonProceduralCellSlots.InterestPoint;
                }

                if (index == layout.ExitCellIndex)
                {
                    expectedSlots |= DungeonProceduralCellSlots.Objective;
                }

                Assert.That(cell.Slots, Is.EqualTo(expectedSlots));
            }
        }

        [Test]
        public void Build_WhenOnlyExitCanAcceptBranch_StopsAfterBoundedAttempts()
        {
            var planner = new DungeonProceduralLayoutPlanner();

            Assert.Throws<DungeonLayoutGenerationException>(() =>
                planner.Build(
                    seed: 1,
                    width: 1,
                    height: 4,
                    targetCellCount: 4,
                    mainRouteCellCount: 3,
                    maxGenerationAttempts: 3));
        }

        [Test]
        public void Build_WhenCountsAreInvalid_RejectsDefinition()
        {
            var planner = new DungeonProceduralLayoutPlanner();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                planner.Build(1, 4, 4, 5, 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                planner.Build(1, 2, 2, 5, 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                planner.Build(1, 4, 4, 5, 3, 0));
        }

        private static void AssertCell(
            DungeonProceduralCell actual,
            DungeonProceduralCell expected)
        {
            Assert.That(actual.GridX, Is.EqualTo(expected.GridX));
            Assert.That(actual.GridZ, Is.EqualTo(expected.GridZ));
            Assert.That(actual.ParentCellIndex, Is.EqualTo(expected.ParentCellIndex));
            Assert.That(actual.IsMainRoute, Is.EqualTo(expected.IsMainRoute));
            Assert.That(actual.Slots, Is.EqualTo(expected.Slots));
        }

        private static long CoordinateKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}

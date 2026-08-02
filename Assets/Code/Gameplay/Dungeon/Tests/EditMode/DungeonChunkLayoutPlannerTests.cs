using System;
using System.Linq;
using DungeonTeam.Gameplay.Dungeon.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonChunkLayoutPlannerTests
    {
        [Test]
        public void Build_WithMandatoryAndPoolChunks_ReturnsConnectedTargetLayout()
        {
            var planner = new DungeonChunkLayoutPlanner();
            var entry = CreateChunk("entry", Port("east", 3f, 0f, DungeonPortDirection.East));
            var mandatory = CreatePassageChunk("mandatory");
            var room = CreatePassageChunk("room");
            var exit = CreateChunk("exit", Port("west", -3f, 0f, DungeonPortDirection.West));

            var layout = planner.Build(
                seed: 42,
                entry,
                new[] { mandatory },
                new[] { room },
                exit,
                targetChunkCount: 5,
                maxGenerationAttempts: 3);

            Assert.That(layout.Placements.Count, Is.EqualTo(5));
            Assert.That(layout.Placements[0].ChunkId, Is.EqualTo("entry"));
            Assert.That(layout.Placements[1].ChunkId, Is.EqualTo("mandatory"));
            Assert.That(layout.Placements.Count(placement => placement.ChunkId == "room"), Is.EqualTo(2));
            Assert.That(layout.Placements[4].ChunkId, Is.EqualTo("exit"));

            for (var index = 1; index < layout.Placements.Count; index++)
            {
                Assert.That(layout.Placements[index].ConnectedToPlacementIndex,
                    Is.InRange(0, index - 1));
            }

            AssertNoOverlaps(layout);
        }

        [Test]
        public void Build_WithSameSeed_ReturnsSameLayout()
        {
            var planner = new DungeonChunkLayoutPlanner();
            var entry = CreateChunk(
                "entry",
                Port("north", 0f, 3f, DungeonPortDirection.North),
                Port("east", 3f, 0f, DungeonPortDirection.East));
            var room = CreatePassageChunk("room");
            var exit = CreateChunk("exit", Port("west", -3f, 0f, DungeonPortDirection.West));

            var first = planner.Build(17, entry, Array.Empty<DungeonChunkMetadata>(),
                new[] { room }, exit, 4, 5);
            var second = planner.Build(17, entry, Array.Empty<DungeonChunkMetadata>(),
                new[] { room }, exit, 4, 5);

            Assert.That(second.Placements, Is.EqualTo(first.Placements));
        }

        [Test]
        public void Build_WhenPortsAreIncompatible_ThrowsGenerationFailure()
        {
            var planner = new DungeonChunkLayoutPlanner();
            var entry = CreateChunk(
                "entry",
                new DungeonChunkPort(
                    "east",
                    "door",
                    3f,
                    0f,
                    DungeonPortDirection.East));
            var exit = CreateChunk(
                "exit",
                new DungeonChunkPort(
                    "west",
                    "ladder",
                    -3f,
                    0f,
                    DungeonPortDirection.West));

            Assert.Throws<DungeonLayoutGenerationException>(() =>
                planner.Build(
                    1,
                    entry,
                    Array.Empty<DungeonChunkMetadata>(),
                    Array.Empty<DungeonChunkMetadata>(),
                    exit,
                    targetChunkCount: 2,
                    maxGenerationAttempts: 2));
        }

        private static DungeonChunkMetadata CreatePassageChunk(string id)
        {
            return CreateChunk(
                id,
                Port("west", -3f, 0f, DungeonPortDirection.West),
                Port("east", 3f, 0f, DungeonPortDirection.East));
        }

        private static DungeonChunkMetadata CreateChunk(
            string id,
            params DungeonChunkPort[] ports)
        {
            return new DungeonChunkMetadata(
                id,
                new DungeonChunkBounds(0f, 0f, 6f, 6f),
                ports);
        }

        private static DungeonChunkPort Port(
            string id,
            float x,
            float z,
            DungeonPortDirection direction)
        {
            return new DungeonChunkPort(id, "door", x, z, direction);
        }

        private static void AssertNoOverlaps(DungeonChunkLayout layout)
        {
            for (var left = 0; left < layout.Placements.Count; left++)
            {
                for (var right = left + 1; right < layout.Placements.Count; right++)
                {
                    Assert.That(
                        layout.Placements[left].WorldBounds.Overlaps(
                            layout.Placements[right].WorldBounds),
                        Is.False,
                        $"Chunks at indices {left} and {right} overlap.");
                }
            }
        }
    }
}

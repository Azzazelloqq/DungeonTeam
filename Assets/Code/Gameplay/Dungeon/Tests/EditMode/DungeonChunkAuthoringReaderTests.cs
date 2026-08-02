using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonChunkAuthoringReaderTests
    {
        [Test]
        public void ReadMetadata_DemoChunks_BuildsConfiguredLayout()
        {
            var entry = ReadMetadata("DungeonChunkEntry", "chunk.demo.entry");
            var mandatory = ReadMetadata("DungeonChunkMandatory", "chunk.demo.mandatory");
            var room = ReadMetadata("DungeonChunkRoom", "chunk.demo.room");
            var exit = ReadMetadata("DungeonChunkExit", "chunk.demo.exit");

            var layout = new DungeonChunkLayoutPlanner().Build(
                42,
                entry,
                new[] { mandatory },
                new[] { room },
                exit,
                targetChunkCount: 5,
                maxGenerationAttempts: 8);

            Assert.That(entry.Ports.Count, Is.EqualTo(1));
            Assert.That(room.Ports.Count, Is.EqualTo(4));
            Assert.That(layout.Placements.Count, Is.EqualTo(5));
        }

        [Test]
        public void ReadPlacements_RepeatedRoomInstances_UsesUniqueRuntimeIds()
        {
            var prefab = LoadPrefab("DungeonChunkRoom");
            var first = Object.Instantiate(prefab);
            var second = Object.Instantiate(prefab);
            try
            {
                var firstData = DungeonChunkAuthoringReader.ReadPlacements(first, "chunk0");
                var secondData = DungeonChunkAuthoringReader.ReadPlacements(second, "chunk1");

                Assert.That(firstData.EnemyPlacements[0].PlacementId,
                    Is.Not.EqualTo(secondData.EnemyPlacements[0].PlacementId));
                Assert.That(firstData.EnemyPlacements[0].AuthoringId,
                    Is.EqualTo(secondData.EnemyPlacements[0].AuthoringId));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static DungeonChunkMetadata ReadMetadata(string prefabName, string chunkId)
        {
            return DungeonChunkAuthoringReader.ReadMetadata(LoadPrefab(prefabName), chunkId);
        }

        private static GameObject LoadPrefab(string prefabName)
        {
            var path = $"Assets/Content/Dungeon/Chunks/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }
    }
}

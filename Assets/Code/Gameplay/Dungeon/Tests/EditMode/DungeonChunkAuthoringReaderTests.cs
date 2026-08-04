using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        [Test]
        public void RequireEntryPose_EntryPrefab_ReturnsEntryPointMarkerPose()
        {
            var prefab = LoadPrefab("DungeonChunkEntry");
            var marker = prefab.GetComponentInChildren<DungeonEntryPointAuthoring>(true);

            var pose = DungeonChunkAuthoringReader.RequireEntryPose(prefab);

            Assert.That(marker, Is.Not.Null);
            Assert.That(pose.PositionX, Is.EqualTo(marker.transform.position.x));
            Assert.That(pose.PositionY, Is.EqualTo(marker.transform.position.y));
            Assert.That(pose.PositionZ, Is.EqualTo(marker.transform.position.z));
        }

        [Test]
        public void RequireExitPose_ExitPrefab_ReturnsExitPointMarkerPose()
        {
            var prefab = LoadPrefab("DungeonChunkExit");
            var marker = prefab.GetComponentInChildren<DungeonExitPointAuthoring>(true);

            var pose = DungeonChunkAuthoringReader.RequireExitPose(prefab);

            Assert.That(marker, Is.Not.Null);
            Assert.That(pose.PositionX, Is.EqualTo(marker.transform.position.x));
            Assert.That(pose.PositionY, Is.EqualTo(marker.transform.position.y));
            Assert.That(pose.PositionZ, Is.EqualTo(marker.transform.position.z));
        }

        [Test]
        public void RequireEntryPose_RegularChunk_ThrowsClearAuthoringError()
        {
            var prefab = LoadPrefab("DungeonChunkRoom");

            var exception = Assert.Throws<InvalidOperationException>(
                () => DungeonChunkAuthoringReader.RequireEntryPose(prefab));

            StringAssert.Contains("exactly one entry point marker", exception.Message);
        }

        [Test]
        public void RequireExitPose_MultipleExitPoints_ThrowsClearAuthoringError()
        {
            var chunk = new GameObject("Chunk");
            chunk.AddComponent<DungeonChunkAuthoring>();
            var firstPoint = new GameObject("ExitPointA");
            var secondPoint = new GameObject("ExitPointB");
            firstPoint.transform.SetParent(chunk.transform);
            secondPoint.transform.SetParent(chunk.transform);
            firstPoint.AddComponent<DungeonExitPointAuthoring>();
            secondPoint.AddComponent<DungeonExitPointAuthoring>();
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonChunkAuthoringReader.RequireExitPose(chunk));

                StringAssert.Contains("exactly one exit point marker", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(chunk);
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

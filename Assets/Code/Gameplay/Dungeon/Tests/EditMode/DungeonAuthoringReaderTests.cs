using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonAuthoringReaderTests
    {
        private const string PrefabPath =
            "Assets/Content/Dungeon/Maps/AuthoredDungeonDemo.prefab";

        [Test]
        public void Read_DemoAuthoredPrefab_ReturnsConfiguredMapData()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var instance = Object.Instantiate(prefab);
            try
            {
                var data = DungeonAuthoringReader.Read(
                    instance,
                    "dungeon.demo.authored",
                    seed: 42);

                Assert.That(data.Snapshot.DungeonId, Is.EqualTo("dungeon.demo.authored"));
                Assert.That(data.Snapshot.Seed, Is.EqualTo(42));
                Assert.That(data.EnemyPlacements, Has.Length.EqualTo(2));
                Assert.That(data.InterestPointPlacements, Has.Length.EqualTo(2));
                Assert.That(data.ObjectivePlacements, Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}

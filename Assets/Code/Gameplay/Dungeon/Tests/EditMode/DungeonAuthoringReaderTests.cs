using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
                Assert.That(data.EnemyPlacements, Has.Length.EqualTo(3));
                AssertOptionalEnemy(
                    FindEnemy(data.EnemyPlacements, "enemy.optional.melee"),
                    "actor.skeleton.warrior",
                    "behavior.enemy.melee.basic",
                    "loadout.skeleton.warrior");
                AssertOptionalEnemy(
                    FindEnemy(data.EnemyPlacements, "enemy.optional.ranged"),
                    "actor.skeleton.mage",
                    "behavior.enemy.ranged.basic",
                    "loadout.skeleton.mage");
                AssertOptionalEnemy(
                    FindEnemy(data.EnemyPlacements, "enemy.optional.area"),
                    "actor.skeleton.mage",
                    "behavior.enemy.ranged.basic",
                    "loadout.skeleton.area");
                Assert.That(data.InterestPointPlacements, Has.Length.EqualTo(1));
                Assert.That(data.InterestPointPlacements[0].PlacementId,
                    Is.EqualTo("interest.optional.chest"));
                Assert.That(data.InterestPointPlacements[0].Mode,
                    Is.EqualTo(DungeonPlacementMode.OptionalFixed));
                Assert.That(data.InterestPointPlacements[0].FixedInterestPointId,
                    Is.EqualTo("interest.chest.basic"));
                Assert.That(data.ObjectivePlacements, Has.Length.EqualTo(1));

                var spatial = data.Snapshot.SpatialLayout;
                Assert.That(spatial.HasAuthoredData, Is.True);
                Assert.That(spatial.RouteCheckpoints, Has.Count.EqualTo(6));
                Assert.That(CountPlanarTurns(spatial.RouteCheckpoints), Is.GreaterThanOrEqualTo(2));
                Assert.That(spatial.CameraShots, Has.Count.EqualTo(2));
                Assert.That(spatial.Encounter.StartCheckpointIndex, Is.EqualTo(3));
                Assert.That(spatial.Encounter.EndCheckpointIndex, Is.EqualTo(5));
                Assert.That(spatial.CompanionFormationOffsets, Has.Count.EqualTo(3));
                Assert.That(spatial.TacticalAnchors, Has.Count.EqualTo(3));
                Assert.That(data.Snapshot.VisibilityLayout.HasAuthoredVisibility, Is.True);
                Assert.That(data.Snapshot.VisibilityLayout.ZoneCount, Is.EqualTo(2));
                Assert.That(data.Snapshot.VisibilityLayout.Doors, Has.Count.EqualTo(1));
                Assert.That(data.Snapshot.VisibilityLayout.Doors[0].RevealedZoneIndex,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Read_CompleteSpatialAuthoring_ReturnsOrderedUnityFreeData()
        {
            var root = CreateMapRoot(out var authoring, out var entry, out var exit);
            var route = new[]
            {
                entry,
                CreateMarker(root.transform, "Route1", new Vector3(0f, 0f, 5f)),
                exit
            };
            var cameraAnchors = new[]
            {
                CreateMarker(root.transform, "Shot0", new Vector3(0f, 3f, 4f)),
                CreateMarker(root.transform, "Shot1", new Vector3(3.2f, 3f, 9f))
            };
            var formationAnchors = new[]
            {
                CreateMarker(root.transform, "Formation0", new Vector3(1f, 0f, -1f)),
                CreateMarker(root.transform, "Formation1", new Vector3(-1f, 0f, -1f))
            };
            var tacticalAnchors = new[]
            {
                CreateMarker(root.transform, "Tactical0", new Vector3(2f, 0f, 7f)),
                CreateMarker(root.transform, "Tactical1", new Vector3(-2f, 0f, 7f))
            };

            ConfigureSpatialAuthoring(
                authoring,
                route,
                cameraAnchors,
                route[0],
                route[2],
                formationAnchors,
                tacticalAnchors);
            root.transform.position = new Vector3(10f, 0f, 20f);

            try
            {
                var snapshot = DungeonAuthoringReader.Read(root, "dungeon.corridor", 7).Snapshot;

                Assert.That(snapshot.SpatialLayout.HasAuthoredData, Is.True);
                Assert.That(snapshot.SpatialLayout.RouteCheckpoints, Has.Count.EqualTo(3));
                Assert.That(snapshot.SpatialLayout.RouteCheckpoints[1].PositionZ, Is.EqualTo(25f));
                Assert.That(snapshot.SpatialLayout.CameraShots, Has.Count.EqualTo(2));
                Assert.That(snapshot.SpatialLayout.CameraShots[0].LookAheadDistance,
                    Is.EqualTo(2f));
                Assert.That(snapshot.SpatialLayout.CameraShots[0].ActivationRange,
                    Is.EqualTo(8f));
                Assert.That(snapshot.SpatialLayout.CameraShots[0].BlendRange,
                    Is.EqualTo(3f));
                Assert.That(snapshot.SpatialLayout.CameraShots[0].RouteCheckpointIndex,
                    Is.EqualTo(1));
                Assert.That(snapshot.SpatialLayout.CameraShots[1].RouteCheckpointIndex,
                    Is.EqualTo(2));
                Assert.That(snapshot.SpatialLayout.Encounter.StartCheckpointIndex,
                    Is.EqualTo(0));
                Assert.That(snapshot.SpatialLayout.Encounter.EndCheckpointIndex,
                    Is.EqualTo(2));
                Assert.That(snapshot.SpatialLayout.CompanionFormationOffsets,
                    Has.Count.EqualTo(2));
                Assert.That(snapshot.SpatialLayout.CompanionFormationOffsets[0].X,
                    Is.EqualTo(1f));
                Assert.That(snapshot.SpatialLayout.TacticalAnchors, Has.Count.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CreateSpatialLayout_SourceArraysMutate_RemainsUnchanged()
        {
            var route = new[]
            {
                Pose(0f, 0f),
                Pose(0f, 5f)
            };
            var cameraShots = new[]
            {
                new DungeonCameraShot(Pose(0f, 2f), 1, 1f, 4f, 2f)
            };
            var formationOffsets = new[] { new DungeonVector3(1f, 0f, -1f) };
            var tacticalAnchors = new[] { Pose(2f, 3f) };
            var layout = new DungeonSpatialLayout(
                route,
                cameraShots,
                new DungeonEncounterSpan(Pose(0f, 1f), Pose(0f, 4f), 0, 1),
                formationOffsets,
                tacticalAnchors);

            route[0] = Pose(99f, 99f);
            cameraShots[0] = new DungeonCameraShot(Pose(99f, 99f), 0, 1f, 4f, 2f);
            formationOffsets[0] = new DungeonVector3(99f, 99f, 99f);
            tacticalAnchors[0] = Pose(99f, 99f);

            Assert.That(layout.RouteCheckpoints[0].PositionX, Is.EqualTo(0f));
            Assert.That(layout.CameraShots[0].RouteCheckpointIndex, Is.EqualTo(1));
            Assert.That(layout.CompanionFormationOffsets[0].X, Is.EqualTo(1f));
            Assert.That(layout.TacticalAnchors[0].PositionX, Is.EqualTo(2f));
        }

        [Test]
        public void Read_SpatialRouteContainsNull_ThrowsClearAuthoringError()
        {
            var root = CreateConfiguredSpatialMap(out var authoring);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("_routeCheckpoints").GetArrayElementAtIndex(1)
                .objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonAuthoringReader.Read(root, "dungeon.corridor", 7));

                StringAssert.Contains("route checkpoint at index 1 is missing", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Read_DuplicateFormationAnchor_ThrowsClearAuthoringError()
        {
            var root = CreateConfiguredSpatialMap(out var authoring);
            var serialized = new SerializedObject(authoring);
            var anchors = serialized.FindProperty("_companionFormationAnchors");
            anchors.GetArrayElementAtIndex(1).objectReferenceValue =
                anchors.GetArrayElementAtIndex(0).objectReferenceValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonAuthoringReader.Read(root, "dungeon.corridor", 7));

                StringAssert.Contains("duplicate companion formation anchor", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Read_CameraShotsOutOfRouteOrder_ThrowsClearAuthoringError()
        {
            var root = CreateConfiguredSpatialMap(out var authoring);
            var serialized = new SerializedObject(authoring);
            var shots = serialized.FindProperty("_cameraShots");
            var firstCheckpoint = shots.GetArrayElementAtIndex(0)
                .FindPropertyRelative("_routeCheckpoint");
            var secondCheckpoint = shots.GetArrayElementAtIndex(1)
                .FindPropertyRelative("_routeCheckpoint");
            var firstValue = firstCheckpoint.objectReferenceValue;
            firstCheckpoint.objectReferenceValue = secondCheckpoint.objectReferenceValue;
            secondCheckpoint.objectReferenceValue = firstValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonAuthoringReader.Read(root, "dungeon.corridor", 7));

                StringAssert.Contains("camera shots must follow route order", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Read_RouteDoesNotStartAtEntry_ThrowsClearAuthoringError()
        {
            var root = CreateConfiguredSpatialMap(out var authoring);
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("_routeCheckpoints").GetArrayElementAtIndex(0)
                .objectReferenceValue = CreateMarker(
                    root.transform,
                    "WrongRouteStart",
                    new Vector3(0f, 0f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonAuthoringReader.Read(root, "dungeon.corridor", 7));

                StringAssert.Contains("route must start at the entry marker", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Read_RouteDoesNotEndAtExit_ThrowsClearAuthoringError()
        {
            var root = CreateConfiguredSpatialMap(out var authoring);
            var serialized = new SerializedObject(authoring);
            var route = serialized.FindProperty("_routeCheckpoints");
            route.GetArrayElementAtIndex(route.arraySize - 1).objectReferenceValue = CreateMarker(
                root.transform,
                "WrongRouteEnd",
                new Vector3(4f, 0f, 11f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DungeonAuthoringReader.Read(root, "dungeon.corridor", 7));

                StringAssert.Contains("route must end at the exit marker", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateConfiguredSpatialMap(out DungeonMapAuthoring authoring)
        {
            var root = CreateMapRoot(out authoring, out var entry, out var exit);
            var route = new[]
            {
                entry,
                CreateMarker(root.transform, "Route1", new Vector3(0f, 0f, 5f)),
                exit
            };
            var cameraAnchors = new[]
            {
                CreateMarker(root.transform, "Shot0", new Vector3(0f, 3f, 4f)),
                CreateMarker(root.transform, "Shot1", new Vector3(3.2f, 3f, 9f))
            };
            ConfigureSpatialAuthoring(
                authoring,
                route,
                cameraAnchors,
                route[0],
                route[2],
                new[]
                {
                    CreateMarker(root.transform, "Formation0", new Vector3(1f, 0f, -1f)),
                    CreateMarker(root.transform, "Formation1", new Vector3(-1f, 0f, -1f))
                },
                new[]
                {
                    CreateMarker(root.transform, "Tactical0", new Vector3(2f, 0f, 7f)),
                    CreateMarker(root.transform, "Tactical1", new Vector3(-2f, 0f, 7f))
                });
            return root;
        }

        private static GameObject CreateMapRoot(
            out DungeonMapAuthoring authoring,
            out Transform entry,
            out Transform exit)
        {
            var root = new GameObject("SpatialDungeon");
            authoring = root.AddComponent<DungeonMapAuthoring>();
            entry = CreateMarker(root.transform, "Entry", Vector3.zero);
            exit = CreateMarker(root.transform, "Exit", new Vector3(4f, 0f, 12f));
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("_entry").objectReferenceValue = entry;
            serialized.FindProperty("_exit").objectReferenceValue = exit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void ConfigureSpatialAuthoring(
            DungeonMapAuthoring authoring,
            Transform[] route,
            Transform[] cameraAnchors,
            Transform encounterStart,
            Transform encounterEnd,
            Transform[] formationAnchors,
            Transform[] tacticalAnchors)
        {
            var serialized = new SerializedObject(authoring);
            SetObjectArray(serialized.FindProperty("_routeCheckpoints"), route);
            var cameraShots = serialized.FindProperty("_cameraShots");
            cameraShots.arraySize = cameraAnchors.Length;
            for (var index = 0; index < cameraAnchors.Length; index++)
            {
                var shot = cameraShots.GetArrayElementAtIndex(index);
                shot.FindPropertyRelative("_anchor").objectReferenceValue = cameraAnchors[index];
                shot.FindPropertyRelative("_routeCheckpoint").objectReferenceValue =
                    route[index + 1];
                shot.FindPropertyRelative("_lookAheadDistance").floatValue = 2f;
                shot.FindPropertyRelative("_activationRange").floatValue = 8f;
                shot.FindPropertyRelative("_blendRange").floatValue = 3f;
            }

            serialized.FindProperty("_encounterStart").objectReferenceValue = encounterStart;
            serialized.FindProperty("_encounterEnd").objectReferenceValue = encounterEnd;
            SetObjectArray(
                serialized.FindProperty("_companionFormationAnchors"),
                formationAnchors);
            SetObjectArray(serialized.FindProperty("_tacticalAnchors"), tacticalAnchors);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedProperty property, Transform[] values)
        {
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent);
            marker.position = position;
            return marker;
        }

        private static DungeonPose Pose(float positionX, float positionZ)
        {
            return new DungeonPose(positionX, 0f, positionZ, 0f, 0f, 0f, 1f);
        }

        private static EnemyPlacement FindEnemy(
            EnemyPlacement[] placements,
            string placementId)
        {
            for (var index = 0; index < placements.Length; index++)
            {
                if (placements[index].PlacementId == placementId)
                {
                    return placements[index];
                }
            }

            Assert.Fail($"Missing enemy placement '{placementId}'.");
            return default;
        }

        private static void AssertOptionalEnemy(
            EnemyPlacement placement,
            string enemyId,
            string behaviorId,
            string loadoutId)
        {
            Assert.That(placement.Mode, Is.EqualTo(DungeonPlacementMode.OptionalFixed));
            Assert.That(placement.FixedEnemyId, Is.EqualTo(enemyId));
            Assert.That(placement.FixedBehaviorId, Is.EqualTo(behaviorId));
            Assert.That(placement.FixedLoadoutId, Is.EqualTo(loadoutId));
            Assert.That(placement.EncounterGroupId, Is.EqualTo("encounter.corridor.final"));
        }

        private static int CountPlanarTurns(
            System.Collections.Generic.IReadOnlyList<DungeonPose> checkpoints)
        {
            var turns = 0;
            for (var index = 1; index < checkpoints.Count - 1; index++)
            {
                var incomingX = checkpoints[index].PositionX - checkpoints[index - 1].PositionX;
                var incomingZ = checkpoints[index].PositionZ - checkpoints[index - 1].PositionZ;
                var outgoingX = checkpoints[index + 1].PositionX - checkpoints[index].PositionX;
                var outgoingZ = checkpoints[index + 1].PositionZ - checkpoints[index].PositionZ;
                var cross = incomingX * outgoingZ - incomingZ * outgoingX;
                if (Mathf.Abs(cross) > 0.01f)
                {
                    turns++;
                }
            }

            return turns;
        }
    }
}

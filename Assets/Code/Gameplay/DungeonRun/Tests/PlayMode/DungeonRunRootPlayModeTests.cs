using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using NUnit.Framework;
using TickHandler.UnityTickHandler;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.PlayMode
{
    public sealed class DungeonRunRootPlayModeTests
    {
        [UnityTest]
        public IEnumerator InitializeAndDispose_WithValidWorld_OwnsWorldGraph()
        {
            var actorPrefabObject = new GameObject("ActorTestPrefab");
            actorPrefabObject.SetActive(false);
            var actorPrefab = actorPrefabObject.AddComponent<TestActorView>();
            var contextActionsPrefabObject = new GameObject(
                "ContextActionsTestPrefab",
                typeof(RectTransform));
            contextActionsPrefabObject.SetActive(false);
            var contextActionsPrefab = contextActionsPrefabObject.AddComponent<ContextActionsView>();
            var contextActionsParentObject = new GameObject(
                "ContextActionsTestParent",
                typeof(RectTransform));
            var contextActionsParent = contextActionsParentObject.GetComponent<RectTransform>();
            var cameraObject = new GameObject("TeamTestCamera");
            var worldCamera = cameraObject.AddComponent<Camera>();
            var dispatcherObject = new GameObject("TeamTestDispatcher");
            var dispatcher = dispatcherObject.AddComponent<UnityDispatcherBehaviour>();
            var tickHandler = new UnityTickHandler(dispatcher);
            var teamInput = new FakeTeamInput();
            var root = new DungeonRunRoot(
                new FakeDungeonFactory(),
                new DungeonBuildRequest("dungeon.test", "scenario.test", "normal", seed: 42),
                new DungeonRunBindings(actorPrefab, contextActionsPrefab),
                contextActionsParent,
                worldCamera,
                tickHandler,
                teamInput,
                new TeamControlSettings(),
                new EnemyAiSettings());

            GameObject mapRoot = null;
            GameObject navigationRoot = null;
            GameObject actorsRoot = null;
            GameObject visionArea = null;
            GameObject contextActions = null;
            try
            {
                yield return root.InitializeAsync(default).ToCoroutine();

                mapRoot = GameObject.Find("DungeonTestMap");
                navigationRoot = GameObject.Find("DungeonRunNavigation");
                actorsRoot = GameObject.Find("DungeonRunActors");
                visionArea = GameObject.Find("EnemyVisionArea");
                contextActions = GameObject.Find("ContextActions");

                Assert.That(root.MapSnapshot.DungeonId, Is.EqualTo("dungeon.test"));
                Assert.That(root.Leader, Is.Not.Null);
                Assert.That(root.Companion, Is.Not.Null);
                Assert.That(root.Enemies, Has.Count.EqualTo(2));
                Assert.That(root.Enemies[0].IsAlive, Is.True);
                Assert.That(root.Enemies[1].IsAlive, Is.True);
                Assert.That(root.Leader.IsAlive, Is.True);
                Assert.That(mapRoot, Is.Not.Null);
                Assert.That(navigationRoot, Is.Not.Null);
                Assert.That(actorsRoot, Is.Not.Null);
                Assert.That(visionArea, Is.Not.Null);
                Assert.That(contextActions, Is.Not.Null);
                Assert.That(actorsRoot.transform.childCount, Is.EqualTo(4));
                Assert.That(actorsRoot.transform.Find("Enemy_enemy.test.a"), Is.Not.Null);
                Assert.That(actorsRoot.transform.Find("Enemy_enemy.test.b"), Is.Not.Null);

                var firstEnemyHealth = root.Enemies[0].CurrentHealth;
                var secondEnemyHealth = root.Enemies[1].CurrentHealth;
                var attackButton = contextActions.GetComponentInChildren<Button>();
                Assert.That(attackButton, Is.Not.Null);

                root.Enemies[0].ApplyDamage(firstEnemyHealth - 12);
                attackButton.onClick.Invoke();
                yield return null;
                yield return null;

                Assert.That(root.Enemies[0].IsAlive, Is.False);
                Assert.That(root.Enemies[1].CurrentHealth, Is.EqualTo(secondEnemyHealth));

                yield return new WaitForSeconds(1.1f);

                Assert.That(root.Enemies[1].CurrentHealth, Is.EqualTo(secondEnemyHealth),
                    "Companion must not automatically chain to the next target.");
            }
            finally
            {
                root.Dispose();
                tickHandler.Dispose();
                Object.Destroy(actorPrefabObject);
                Object.Destroy(contextActionsPrefabObject);
                Object.Destroy(contextActionsParentObject);
                Object.Destroy(cameraObject);
                Object.Destroy(dispatcherObject);
            }

            yield return null;

            Assert.That(mapRoot == null, Is.True);
            Assert.That(navigationRoot == null, Is.True);
            Assert.That(actorsRoot == null, Is.True);
            Assert.That(visionArea == null, Is.True);
            Assert.That(contextActions == null, Is.True);
            Assert.That(teamInput.IsDisposed, Is.True);
        }

        private sealed class FakeDungeonFactory : IDungeonFactory
        {
            public UniTask<IDungeonInstance> CreateAsync(
                DungeonBuildRequest request,
                CancellationToken ownerToken)
            {
                ownerToken.ThrowIfCancellationRequested();
                return UniTask.FromResult<IDungeonInstance>(new FakeDungeonInstance(request));
            }
        }

        private sealed class FakeDungeonInstance : IDungeonInstance
        {
            private GameObject _mapRoot;

            public FakeDungeonInstance(DungeonBuildRequest request)
            {
                _mapRoot = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _mapRoot.name = "DungeonTestMap";
                _mapRoot.transform.localScale = new Vector3(2f, 1f, 2f);

                var entryPose = Pose(0f, 0f);
                MapSnapshot = new DungeonMapSnapshot(
                    request.DungeonId,
                    request.Seed,
                    entryPose,
                    Pose(5f, 5f));
                ContentPlan = new DungeonContentPlan(
                    new[]
                    {
                        new EnemySpawnPlan("enemy.test.a", "enemy.grunt", "", Pose(3f, 3f)),
                        new EnemySpawnPlan("enemy.test.b", "enemy.grunt", "", Pose(-3f, 3f))
                    },
                    new InterestPointSpawnPlan[0],
                    new ObjectiveSpawnPlan[0],
                    rewardBudgetMultiplier: 1f);
            }

            public DungeonMapSnapshot MapSnapshot { get; }

            public DungeonContentPlan ContentPlan { get; }

            public void Dispose()
            {
                var mapRoot = _mapRoot;
                _mapRoot = null;
                if (mapRoot != null)
                {
                    Object.Destroy(mapRoot);
                }
            }

            private static DungeonPose Pose(float x, float z)
            {
                return new DungeonPose(x, 0f, z, 0f, 0f, 0f, 1f);
            }
        }

        public sealed class TestActorView : ActorViewBase
        {
            public override Vector3 Position => transform.position;

            public override Vector3 Forward => transform.forward;

            public override bool IsOnNavMesh => true;

            public override void Configure(Color color, float movementSpeed)
            {
            }

            public override bool TryMoveTo(Vector3 destination)
            {
                transform.position = destination;
                return true;
            }

            public override bool SetMoveDirection(Vector3 direction)
            {
                transform.position += direction;
                return true;
            }

            public override void StopMovement()
            {
            }

            public override void PlayAttackFeedback()
            {
            }

            public override void SetTargetHighlighted(bool isHighlighted)
            {
            }

            public override void PlayDamageFeedback(int amount)
            {
            }

            public override void PlayDeathFeedback()
            {
            }

            protected override void OnInitialize()
            {
            }

            protected override ValueTask OnInitializeAsync(CancellationToken token)
            {
                return default;
            }

            protected override void OnDispose()
            {
            }

            protected override ValueTask OnDisposeAsync(CancellationToken token)
            {
                return default;
            }
        }

        private sealed class FakeTeamInput : ITeamInput
        {
            public Vector2 Movement => Vector2.zero;

            public float CameraYawDelta => 0f;

            public bool IsDisposed { get; private set; }

            public void Enable()
            {
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}

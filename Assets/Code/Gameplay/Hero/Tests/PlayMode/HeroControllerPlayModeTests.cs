using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Domain;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using NUnit.Framework;
using TickHandler.UnityTickHandler;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.Hero.Runtime.Tests.PlayMode
{
    public sealed class HeroControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator BasicAttack_WhenTargetIsOutOfRange_ApproachesAndHitsOnce()
        {
            var world = new TestWorld();
            try
            {
                Assert.That(world.Controller.TrySetTarget(world.Enemy), Is.True);
                Assert.That(world.Controller.TryRequestBasicAttack(), Is.True);

                yield return null;
                yield return null;

                Assert.That(world.Hero.Position.x, Is.GreaterThan(0f));
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(40));

                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(40),
                    "One attack request must execute exactly one hit.");
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ManualMovement_CancelsApproachButKeepsSelectedTarget()
        {
            var world = new TestWorld();
            try
            {
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestBasicAttack();
                world.Input.Movement = Vector2.left;

                yield return null;

                world.Input.Movement = Vector2.zero;
                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
                Assert.That(world.Controller.CanAttack, Is.True);
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Dispose_UnsubscribesInputDrivenMovement()
        {
            var world = new TestWorld();
            try
            {
                var positionBeforeDispose = world.Hero.Position;
                world.Controller.Dispose();
                world.Input.Movement = Vector2.right;

                yield return null;

                Assert.That(world.Hero.Position, Is.EqualTo(positionBeforeDispose));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator SelectedTarget_WhenItDies_IsCleared()
        {
            var world = new TestWorld();
            try
            {
                world.Controller.TrySetTarget(world.Enemy);
                world.Enemy.ApplyDamage(world.Enemy.CurrentHealth);

                yield return null;

                Assert.That(world.Controller.Target, Is.Null);
                Assert.That(world.Controller.CanAttack, Is.False);
            }
            finally
            {
                world.Dispose();
            }
        }

        private sealed class TestWorld
        {
            private readonly GameObject _actorPrefabObject;
            private readonly GameObject _cameraObject;
            private readonly GameObject _dispatcherObject;
            private readonly UnityTickHandler _tickHandler;

            public TestWorld()
            {
                _actorPrefabObject = new GameObject("HeroTestActorPrefab");
                _actorPrefabObject.SetActive(false);
                var actorPrefab = _actorPrefabObject.AddComponent<TestActorView>();

                var factory = new ActorFactory();
                Hero = factory.Create(
                    new ActorDefinition(
                        "actor.hero.test",
                        actorPrefab),
                    new ActorRuntimeDefinition(
                        "actor.hero.test",
                        level: 1,
                        maximumHealth: 100,
                        movementSpeed: 4f,
                        combatLoadoutId: "loadout.test",
                        primaryAttackRank: 1),
                    new ActorSpawnRequest(
                        "Hero",
                        Vector3.zero,
                        Quaternion.identity));
                Enemy = factory.Create(
                    new ActorDefinition(
                        "actor.enemy.test",
                        actorPrefab),
                    new ActorRuntimeDefinition(
                        "actor.enemy.test",
                        level: 1,
                        maximumHealth: 60,
                        movementSpeed: 3f,
                        combatLoadoutId: "loadout.test",
                        primaryAttackRank: 1),
                    new ActorSpawnRequest(
                        "Enemy",
                        new Vector3(5f, 0f, 0f),
                        Quaternion.identity));

                _cameraObject = new GameObject("HeroTestCamera");
                var camera = _cameraObject.AddComponent<Camera>();
                camera.transform.rotation = Quaternion.identity;

                _dispatcherObject = new GameObject("HeroTestDispatcher");
                var dispatcher = _dispatcherObject.AddComponent<UnityDispatcherBehaviour>();
                _tickHandler = new UnityTickHandler(dispatcher);
                Input = new FakeHeroInput();
                Controller = new HeroController(
                    Hero,
                    new[] { Enemy },
                    camera,
                    _tickHandler,
                    Input,
                    new HeroControlSettings(),
                    new ActorCombatController(
                        Hero,
                        new AttackRankDefinition(
                            rank: 1,
                            damage: 20,
                            range: 1.5f,
                            cooldown: 0.8f)));
                Controller.Initialize();
            }

            public ActorInstance Hero { get; }
            public ActorInstance Enemy { get; }
            public FakeHeroInput Input { get; }
            public HeroController Controller { get; }

            public void Dispose()
            {
                Controller.Dispose();
                Hero.Dispose();
                Enemy.Dispose();
                _tickHandler.Dispose();
                Object.Destroy(_actorPrefabObject);
                Object.Destroy(_cameraObject);
                Object.Destroy(_dispatcherObject);
            }
        }

        private sealed class FakeHeroInput : IHeroInput
        {
            public Vector2 Movement { get; set; }
            public bool TargetSelectionWasPressed => false;
            public Vector2 PointerPosition => Vector2.zero;
            public bool BasicAttackWasPressed => false;
        }

        public sealed class TestActorView : ActorViewBase
        {
            public override Vector3 Position => transform.position;
            public override Vector3 Forward => transform.forward;
            public override bool IsOnNavMesh => true;

            public override Transform WeaponAnchor => null;

            public override Transform HitVfxAnchor => null;

            public override Transform OverheadAnchor => null;

            public override void Configure(float movementSpeed)
            {
            }

            public override bool TryMoveTo(Vector3 destination)
            {
                var direction = destination - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 1f)
                {
                    transform.position = destination - direction.normalized;
                }

                return true;
            }

            public override bool SetMoveDirection(Vector3 direction)
            {
                transform.position += direction;
                return true;
            }

            public override bool TryFaceTowards(Vector3 targetPosition)
            {
                var direction = targetPosition - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    return false;
                }

                transform.rotation = Quaternion.LookRotation(direction);
                return true;
            }

            public override void StopMovement()
            {
            }

            public override void PlayAttackFeedback()
            {
            }

            public override void PlayDamageFeedback(int amount, bool isFatal)
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
    }
}

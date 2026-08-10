using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Combat.Tests
{
    public sealed class ActorCombatControllerTests
    {
        [Test]
        public void TryUse_TwoSlots_HaveIndependentCooldowns()
        {
            using var world = new TestWorld(targetPosition: Vector3.right);

            Assert.That(
                world.Combat.TryUse(SkillSlot.Primary, world.Target, true),
                Is.EqualTo(SkillUseResult.Executed));
            Assert.That(
                world.Combat.TryUse(SkillSlot.Active1, world.Target, true),
                Is.EqualTo(SkillUseResult.Executed));
            Assert.That(
                world.Combat.TryUse(SkillSlot.Primary, world.Target, true),
                Is.EqualTo(SkillUseResult.OnCooldown));
            Assert.That(world.Target.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void TryUse_TargetOutsideRange_ReturnsOutOfRangeWithoutCooldown()
        {
            using var world = new TestWorld(targetPosition: Vector3.right * 3f);

            var result = world.Combat.TryUse(SkillSlot.Primary, world.Target, true);

            Assert.That(result, Is.EqualTo(SkillUseResult.OutOfRange));
            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.True);
            Assert.That(world.Target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void TryUse_BlockedTarget_ReturnsBlockedWithoutCooldown()
        {
            using var world = new TestWorld(targetPosition: Vector3.right);

            var result = world.Combat.TryUse(SkillSlot.Primary, world.Target, false);

            Assert.That(result, Is.EqualTo(SkillUseResult.Blocked));
            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.True);
        }

        [Test]
        public void TryUse_NullOrSelfTarget_ReturnsInvalidTarget()
        {
            using var world = new TestWorld(targetPosition: Vector3.right);

            Assert.That(
                world.Combat.TryUse(SkillSlot.Primary, null, true),
                Is.EqualTo(SkillUseResult.InvalidTarget));
            Assert.That(
                world.Combat.TryUse(SkillSlot.Primary, world.Source, true),
                Is.EqualTo(SkillUseResult.InvalidTarget));
        }

        [Test]
        public void TryUse_DeadTarget_ReturnsInvalidTargetWithoutCooldown()
        {
            using var world = new TestWorld(targetPosition: Vector3.right);
            world.Target.ApplyDamage(world.Target.CurrentHealth);

            var result = world.Combat.TryUse(SkillSlot.Primary, world.Target, true);

            Assert.That(result, Is.EqualTo(SkillUseResult.InvalidTarget));
            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.True);
        }

        [Test]
        public void CancelActiveUse_BeforeCommit_DoesNotStartCooldownOrDealDamage()
        {
            using var world = new TestWorld(
                targetPosition: Vector3.right,
                commitDelay: 0.4f);

            Assert.That(
                world.Combat.TryUse(SkillSlot.Primary, world.Target, true),
                Is.EqualTo(SkillUseResult.Executed));
            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.False);

            Assert.That(world.Combat.CancelActiveUse(), Is.True);

            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.True);
            Assert.That(world.Target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Tick_ReachesCommit_StartsCooldownAndAppliesMechanic()
        {
            using var world = new TestWorld(
                targetPosition: Vector3.right,
                commitDelay: 0.4f);
            world.Combat.TryUse(SkillSlot.Primary, world.Target, true);

            world.Tick(0.4f);

            Assert.That(world.Target.CurrentHealth, Is.EqualTo(90));
            Assert.That(world.Combat.IsReady(SkillSlot.Primary), Is.False);
            Assert.That(world.Combat.CancelActiveUse(), Is.False,
                "Committed mechanics cannot be rolled back by movement cancellation.");
        }

        private sealed class TestWorld : IDisposable
        {
            private readonly GameObject _prefabObject;
            private readonly GameObject _projectilesRoot;
            private readonly TestDispatcher _dispatcher;
            private readonly UnityTickHandler _tickHandler;
            private readonly SkillViewSet _views;
            private readonly SkillExecutionController _execution;

            public TestWorld(Vector3 targetPosition, float commitDelay = 0f)
            {
                _prefabObject = new GameObject("CombatTestActorPrefab");
                _prefabObject.SetActive(false);
                var prefab = _prefabObject.AddComponent<TestActorView>();
                var factory = new ActorFactory();
                Source = CreateActor(factory, prefab, "actor.source", Vector3.zero);
                Target = CreateActor(factory, prefab, "actor.target", targetPosition);

                _dispatcher = new TestDispatcher();
                _tickHandler = new UnityTickHandler(_dispatcher);
                _views = new SkillViewSet(
                    Array.Empty<SkillProjectileViewEntry>(),
                    new[]
                    {
                        new SkillPresentationViewEntry(
                            "skill.test",
                            EmptySequence())
                    });
                _projectilesRoot = new GameObject("CombatTestProjectiles");
                _execution = new SkillExecutionController(
                    _views,
                    _tickHandler,
                    _projectilesRoot.transform);
                _execution.Initialize();
                Combat = new ActorCombatController(
                    Source,
                    CreateCatalog(commitDelay),
                    "loadout.test",
                    _execution);
            }

            public ActorInstance Source { get; }
            public ActorInstance Target { get; }
            public ActorCombatController Combat { get; }

            public void Tick(float deltaTime)
            {
                _dispatcher.RaiseUpdate(deltaTime);
                Combat.Tick(deltaTime);
            }

            public void Dispose()
            {
                Combat.Dispose();
                _execution.Dispose();
                Source.Dispose();
                Target.Dispose();
                _views.Dispose();
                _tickHandler.Dispose();
                _dispatcher.Dispose();
                UnityEngine.Object.DestroyImmediate(_prefabObject);
                UnityEngine.Object.DestroyImmediate(_projectilesRoot);
            }

            private static ActorInstance CreateActor(
                ActorFactory factory,
                ActorViewBase prefab,
                string actorId,
                Vector3 position)
            {
                return factory.Create(
                    new ActorDefinition(actorId, prefab),
                    new ActorRuntimeDefinition(actorId, 1, 100, 4f),
                    new ActorSpawnRequest(actorId, position, Quaternion.identity));
            }

            private static SkillCatalog CreateCatalog(float commitDelay)
            {
                return new SkillCatalog(
                    new[]
                    {
                        new DirectDamageSkillDefinitionConfig(
                            "skill.test",
                            "Test",
                            SkillTargetRule.EnemyActor,
                            new[]
                            {
                                new DirectDamageSkillLevelConfig(
                                    1,
                                    10,
                                    1.5f,
                                    1f,
                                    commitDelay,
                                    recoveryDuration: commitDelay > 0f ? 0.2f : 0f)
                            })
                    },
                    Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                    new[]
                    {
                        new CombatLoadoutDefinitionConfig(
                            "loadout.test",
                            new[]
                            {
                                new CombatLoadoutSlotConfig(SkillSlot.Primary, "skill.test", 1),
                                new CombatLoadoutSlotConfig(SkillSlot.Active1, "skill.test", 1)
                            })
                    });
            }

            private static SkillPresentationSequence EmptySequence()
            {
                return new SkillPresentationSequence(
                    Array.Empty<SkillActorAnimationCue>(),
                    Array.Empty<SkillVfxCue>());
            }
        }

        private sealed class TestDispatcher : IDispatcher
        {
            public event Action<float> OnUpdate;
            public event Action<float> OnLateUpdate;
            public event Action<float> OnFixedUpdate;
            public event Action<float> OnEndFrameUpdate;

            public float DeltaTime { get; private set; }

            public void RaiseUpdate(float deltaTime)
            {
                DeltaTime = deltaTime;
                OnUpdate?.Invoke(deltaTime);
            }

            public void Dispose()
            {
                OnUpdate = null;
                OnLateUpdate = null;
                OnFixedUpdate = null;
                OnEndFrameUpdate = null;
            }
        }

        private sealed class TestActorView : ActorViewBase
        {
            private int _health;

            public override Vector3 Position => transform.position;
            public override Vector3 Forward => transform.forward;
            public override bool IsOnNavMesh => true;
            public override Transform WeaponAnchor => transform;
            public override Transform HitVfxAnchor => transform;
            public override Transform OverheadAnchor => transform;
            public override Transform SkillOriginAnchor => transform;
            public override void Configure(float movementSpeed) { }
            public override bool TryMoveTo(Vector3 destination) { transform.position = destination; return true; }
            public override bool SetMoveDirection(Vector3 direction) { transform.position += direction; return true; }
            public override bool TryFaceTowards(Vector3 targetPosition) => true;
            public override void StopMovement() { }
            public override void PlayAttackFeedback() { }
            public override void PlayCastFeedback() { }
            public override void PlayDamageFeedback(int amount, bool isFatal) { _health -= amount; }
            public override void PlayDeathFeedback() { }
            protected override void OnInitialize() { _health = 100; }
            protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
            protected override void OnDispose() { }
            protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
        }
    }
}

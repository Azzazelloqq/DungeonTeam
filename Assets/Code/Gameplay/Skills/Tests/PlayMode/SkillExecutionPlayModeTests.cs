using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using NUnit.Framework;
using ResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime.Tests.PlayMode
{
    public sealed class SkillExecutionPlayModeTests
    {
        [Test]
        public void Fireball_TicksToTarget_DealsDamageExactlyOnce()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);

            world.ExecuteFireball(source, target);
            Assert.That(world.Execution.ActiveProjectileCount, Is.EqualTo(1));

            world.Tick(0.5f);
            world.Tick(0.5f);

            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(86));
        }

        [Test]
        public void Fireball_AfterCommit_ContinuesWhenSourceDies()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);
            world.ExecuteFireball(source, target);

            source.ApplyDamage(source.CurrentHealth);
            world.Tick(0.5f);
            world.Tick(0.5f);

            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(86));
        }

        [Test]
        public void Fireball_Spawn_PreservesAuthoredProjectileRotation()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);

            world.ExecuteFireball(source, target);

            Assert.That(
                world.RequireProjectileRotation().eulerAngles.x,
                Is.EqualTo(25f).Within(0.01f));
        }

        [Test]
        public void Fireball_BeforeCommit_DoesNotCreateProjectileOrDealDamage()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);

            var handle = world.BeginFireball(
                source,
                target,
                new SkillUseTiming(0.4f, 0.2f));
            world.Tick(0.39f);

            Assert.That(handle.Phase, Is.EqualTo(SkillUsePhase.Preparing));
            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(100));

            world.Tick(0.01f);

            Assert.That(handle.HasCommitted, Is.True);
            Assert.That(world.Execution.ActiveProjectileCount, Is.EqualTo(1));
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Cancel_BeforeCommit_CleansPresentationAndNeverCreatesProjectile()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);
            var handle = world.BeginFireball(
                source,
                target,
                new SkillUseTiming(0.4f, 0.2f));
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.EqualTo(1));

            Assert.That(handle.TryCancel(), Is.True);
            world.Tick(1f);

            Assert.That(handle.Phase, Is.EqualTo(SkillUsePhase.Cancelled));
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.Zero);
            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void PresentationCue_DelayIsPhaseRelative_AndCancellationCleansIt()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right);
            var handle = world.BeginDelayedStrike(source, target);

            Assert.That(world.Execution.ActivePresentationVfxCount, Is.Zero);
            world.Tick(0.19f);
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.Zero);

            world.Tick(0.01f);
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.EqualTo(1));

            Assert.That(handle.TryCancel(), Is.True);
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Cancel_AfterProjectileCommit_DoesNotRollbackProjectile()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);
            var handle = world.BeginFireball(
                source,
                target,
                new SkillUseTiming(0.2f, 0.5f));
            world.Tick(0.2f);

            var cancelled = handle.TryCancel();
            world.Tick(0.25f);

            Assert.That(cancelled, Is.False);
            Assert.That(handle.HasCommitted, Is.True);
            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(86));
        }

        [Test]
        public void DirectDamage_OnCommit_PlaysImpactAtActualTargetPosition()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);

            world.BeginStrike(
                source,
                target,
                new SkillUseTiming(0.2f, 0.1f));

            Assert.That(target.CurrentHealth, Is.EqualTo(100));
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.Zero);

            world.Tick(0.2f);

            Assert.That(target.CurrentHealth, Is.EqualTo(90));
            Assert.That(world.Execution.ActivePresentationVfxCount, Is.EqualTo(1));
            Assert.That(world.RequireImpactVfxPosition(), Is.EqualTo(target.Position));
            Assert.That(
                world.RequireImpactVfxRotation().eulerAngles.y,
                Is.EqualTo(35f).Within(0.01f));
        }

        [Test]
        public void DirectHeal_OnCommit_HealsOnceAndPlaysImpactAtTarget()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);
            target.ApplyDamage(40);

            world.BeginHeal(source, target, new SkillUseTiming(0.2f, 0.1f));
            world.Tick(0.2f);

            Assert.That(target.CurrentHealth, Is.EqualTo(85));
            Assert.That(world.RequireImpactVfxPosition(), Is.EqualTo(target.Position));

            world.Tick(0.5f);
            Assert.That(target.CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void Fireball_SameSkillView_WorksForTwoActorPrefabVariants()
        {
            using var world = new TestWorld();
            var firstSource = world.CreateActor<TestActorViewA>("actor.first", Vector3.zero);
            var secondSource = world.CreateActor<TestActorViewB>("actor.second", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 2f);

            world.ExecuteFireball(firstSource, target);
            world.Tick(0.5f);
            world.ExecuteFireball(secondSource, target);
            world.Tick(0.5f);

            Assert.That(target.CurrentHealth, Is.EqualTo(72));
            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
        }

        [Test]
        public void Dispose_DuringFlight_RemovesProjectileAndTickSubscription()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right * 20f);
            world.ExecuteFireball(source, target);

            world.Execution.Dispose();
            world.Tick(1f);

            Assert.That(world.Execution.ActiveProjectileCount, Is.Zero);
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Execute_UnsupportedMechanic_ThrowsFailFast()
        {
            using var world = new TestWorld();
            var source = world.CreateActor<TestActorViewA>("actor.source", Vector3.zero);
            var target = world.CreateActor<TestActorViewA>("actor.target", Vector3.right);
            var level = new DirectDamageSkillLevelDefinition(1, 10, 2f, 1f);
            var skill = new UnsupportedSkillDefinition(level);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                world.Execution.Execute(source, target, skill, level));

            StringAssert.Contains(nameof(UnsupportedSkillDefinition), exception.Message);
        }

        [Test]
        public async Task SkillViewSet_ReleasesLoadedAssetsOnlyAfterExecutionsAreDisposed()
        {
            var projectilePrefabObject = new GameObject("ProjectilePrefab");
            projectilePrefabObject.SetActive(false);
            projectilePrefabObject.AddComponent<SkillProjectileView>();
            var resourceLoader = new FakeResourceLoader(projectilePrefabObject);
            var catalog = CreateCatalog();
            var loader = new SkillViewLoader(catalog, resourceLoader);
            var views = await loader.LoadAsync(
                new[] { "loadout.fireball" },
                CancellationToken.None);
            var dispatcher = new TestDispatcher();
            var tickHandler = new UnityTickHandler(dispatcher);
            var projectilesRoot = new GameObject("Projectiles");
            var execution = new SkillExecutionController(
                views,
                tickHandler,
                projectilesRoot.transform);
            execution.Initialize();
            var actorPrefabObject = new GameObject("ActorPrefab");
            actorPrefabObject.SetActive(false);
            var actorPrefab = actorPrefabObject.AddComponent<TestActorViewA>();
            var actorFactory = new ActorFactory();
            var source = actorFactory.Create(
                new ActorDefinition("actor.source", actorPrefab),
                new ActorRuntimeDefinition("actor.source", 1, 100, 4f),
                new ActorSpawnRequest("Source", Vector3.zero, Quaternion.identity));
            var target = actorFactory.Create(
                new ActorDefinition("actor.target", actorPrefab),
                new ActorRuntimeDefinition("actor.target", 1, 100, 4f),
                new ActorSpawnRequest("Target", Vector3.right * 20f, Quaternion.identity));
            var resolved = catalog.Resolve("loadout.fireball", SkillSlot.Primary);
            execution.Execute(source, target, resolved.Skill, resolved.Level);
            Assert.That(execution.ActiveProjectileCount, Is.EqualTo(1));

            execution.Dispose();
            Assert.That(execution.ActiveProjectileCount, Is.Zero);
            Assert.That(resourceLoader.ReleaseCount, Is.Zero);

            source.Dispose();
            target.Dispose();
            views.Dispose();
            Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(3));

            tickHandler.Dispose();
            dispatcher.Dispose();
            resourceLoader.Dispose();
            UnityEngine.Object.DestroyImmediate(actorPrefabObject);
            UnityEngine.Object.DestroyImmediate(projectilesRoot);
            UnityEngine.Object.DestroyImmediate(projectilePrefabObject);
        }

        private static SkillCatalog CreateCatalog()
        {
            return new SkillCatalog(
                Array.Empty<DirectDamageSkillDefinitionConfig>(),
                new[]
                {
                    new ProjectileDamageSkillDefinitionConfig(
                        "skill.fireball",
                        "Fireball",
                        SkillTargetRule.EnemyActor,
                        new[] { new ProjectileDamageSkillLevelConfig(1, 14, 6f, 1.2f, 8f) })
                },
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[]
                {
                    new CombatLoadoutDefinitionConfig(
                        "loadout.fireball",
                        new[]
                        {
                            new CombatLoadoutSlotConfig(
                                SkillSlot.Primary,
                                "skill.fireball",
                                1)
                        })
                });
        }

        private sealed class TestWorld : IDisposable
        {
            private readonly List<GameObject> _prefabObjects = new();
            private readonly List<ActorInstance> _actors = new();
            private readonly GameObject _projectilePrefabObject;
            private readonly GameObject _projectilesRoot;
            private readonly GameObject _presentationPrefabObject;
            private readonly SkillViewSet _views;
            private readonly TestDispatcher _dispatcher;
            private readonly UnityTickHandler _tickHandler;
            private readonly ActorFactory _actorFactory = new();

            public TestWorld()
            {
                _projectilePrefabObject = new GameObject("ProjectilePrefab");
                _projectilePrefabObject.SetActive(false);
                _projectilePrefabObject.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                var projectilePrefab = _projectilePrefabObject.AddComponent<SkillProjectileView>();
                _presentationPrefabObject = new GameObject("PresentationPrefab");
                _presentationPrefabObject.SetActive(false);
                _presentationPrefabObject.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                _views = new SkillViewSet(new[]
                {
                    new SkillProjectileViewEntry("skill.fireball", projectilePrefab)
                }, new[]
                {
                    new SkillPresentationViewEntry(
                        "skill.fireball",
                        new SkillPresentationSequence(
                            new[]
                            {
                                new SkillActorAnimationCue(
                                    SkillPresentationPhase.Start,
                                    0f,
                                    ActorSkillAnimationCue.Cast)
                            },
                            new[]
                            {
                                new SkillVfxCue(
                                    SkillPresentationPhase.Start,
                                    0f,
                                    1f,
                                    SkillVfxAnchor.SourceOrigin,
                                    followAnchor: true,
                                    _presentationPrefabObject),
                                new SkillVfxCue(
                                    SkillPresentationPhase.Impact,
                                    0f,
                                    0.2f,
                                    SkillVfxAnchor.ImpactPosition,
                                    followAnchor: false,
                                    _presentationPrefabObject)
                            })),
                    new SkillPresentationViewEntry(
                        "skill.strike",
                        new SkillPresentationSequence(
                            new[]
                            {
                                new SkillActorAnimationCue(
                                    SkillPresentationPhase.Start,
                                    0f,
                                    ActorSkillAnimationCue.Attack)
                            },
                            new[]
                            {
                                new SkillVfxCue(
                                    SkillPresentationPhase.Impact,
                                    0f,
                                    0.2f,
                                    SkillVfxAnchor.ImpactPosition,
                                    followAnchor: false,
                                    _presentationPrefabObject)
                            })),
                    new SkillPresentationViewEntry(
                        "skill.heal",
                        new SkillPresentationSequence(
                            new[]
                            {
                                new SkillActorAnimationCue(
                                    SkillPresentationPhase.Start,
                                    0f,
                                    ActorSkillAnimationCue.Cast)
                            },
                            new[]
                            {
                                new SkillVfxCue(
                                    SkillPresentationPhase.Impact,
                                    0f,
                                    0.2f,
                                    SkillVfxAnchor.ImpactPosition,
                                    followAnchor: false,
                                    _presentationPrefabObject)
                            })),
                    new SkillPresentationViewEntry(
                        "skill.delayed",
                        new SkillPresentationSequence(
                            Array.Empty<SkillActorAnimationCue>(),
                            new[]
                            {
                                new SkillVfxCue(
                                    SkillPresentationPhase.Start,
                                    0.2f,
                                    1f,
                                    SkillVfxAnchor.SourceOrigin,
                                    followAnchor: true,
                                    _presentationPrefabObject)
                            }))
                });
                _dispatcher = new TestDispatcher();
                _tickHandler = new UnityTickHandler(_dispatcher);
                _projectilesRoot = new GameObject("Projectiles");
                Execution = new SkillExecutionController(
                    _views,
                    _tickHandler,
                    _projectilesRoot.transform);
                Execution.Initialize();
            }

            public SkillExecutionController Execution { get; }

            public ActorInstance CreateActor<TView>(string actorId, Vector3 position)
                where TView : TestActorViewBase
            {
                var prefabObject = new GameObject($"{actorId}.Prefab");
                prefabObject.SetActive(false);
                var prefab = prefabObject.AddComponent<TView>();
                _prefabObjects.Add(prefabObject);
                var actor = _actorFactory.Create(
                    new ActorDefinition(actorId, prefab),
                    new ActorRuntimeDefinition(actorId, 1, 100, 4f),
                    new ActorSpawnRequest(actorId, position, Quaternion.identity));
                _actors.Add(actor);
                return actor;
            }

            public void ExecuteFireball(ActorInstance source, ActorInstance target)
            {
                BeginFireball(source, target, default);
            }

            public SkillUseHandle BeginFireball(
                ActorInstance source,
                ActorInstance target,
                SkillUseTiming timing)
            {
                var level = new ProjectileDamageSkillLevelDefinition(
                    1,
                    14,
                    6f,
                    1.2f,
                    8f,
                    timing);
                var skill = new ProjectileDamageSkillDefinition(
                    "skill.fireball",
                    "Fireball",
                    SkillTargetRule.EnemyActor,
                    new[] { level });
                return Execution.Begin(source, target, skill, level);
            }

            public SkillUseHandle BeginStrike(
                ActorInstance source,
                ActorInstance target,
                SkillUseTiming timing)
            {
                var level = new DirectDamageSkillLevelDefinition(
                    1,
                    10,
                    2f,
                    1f,
                    timing);
                var skill = new DirectDamageSkillDefinition(
                    "skill.strike",
                    "Strike",
                    SkillTargetRule.EnemyActor,
                    new[] { level });
                return Execution.Begin(source, target, skill, level);
            }

            public SkillUseHandle BeginHeal(
                ActorInstance source,
                ActorInstance target,
                SkillUseTiming timing)
            {
                var level = new DirectHealSkillLevelDefinition(
                    1,
                    25,
                    6f,
                    1.2f,
                    timing);
                var skill = new DirectHealSkillDefinition(
                    "skill.heal",
                    "Heal",
                    SkillTargetRule.AllyOrSelfActor,
                    new[] { level });
                return Execution.Begin(source, target, skill, level);
            }

            public SkillUseHandle BeginDelayedStrike(
                ActorInstance source,
                ActorInstance target)
            {
                var level = new DirectDamageSkillLevelDefinition(
                    1,
                    10,
                    2f,
                    1f,
                    new SkillUseTiming(commitDelay: 0.5f, recoveryDuration: 0.1f));
                var skill = new DirectDamageSkillDefinition(
                    "skill.delayed",
                    "Delayed Strike",
                    SkillTargetRule.EnemyActor,
                    new[] { level });
                return Execution.Begin(source, target, skill, level);
            }

            public Quaternion RequireProjectileRotation()
            {
                for (var index = 0; index < _projectilesRoot.transform.childCount; index++)
                {
                    var child = _projectilesRoot.transform.GetChild(index);
                    if (child.name.StartsWith("SkillProjectile_", StringComparison.Ordinal))
                        return child.rotation;
                }

                throw new InvalidOperationException("Projectile instance was not found.");
            }

            public Vector3 RequireImpactVfxPosition()
            {
                return RequireImpactVfx().position;
            }

            public Quaternion RequireImpactVfxRotation()
            {
                return RequireImpactVfx().rotation;
            }

            private Transform RequireImpactVfx()
            {
                for (var index = 0; index < _projectilesRoot.transform.childCount; index++)
                {
                    var child = _projectilesRoot.transform.GetChild(index);
                    if (child.name.StartsWith("SkillVfx_Impact", StringComparison.Ordinal))
                        return child;
                }

                throw new InvalidOperationException("Impact VFX instance was not found.");
            }

            public void Tick(float deltaTime)
            {
                _dispatcher.RaiseUpdate(deltaTime);
            }

            public void Dispose()
            {
                Execution.Dispose();
                for (var index = _actors.Count - 1; index >= 0; index--)
                {
                    _actors[index].Dispose();
                }

                _views.Dispose();
                _tickHandler.Dispose();
                _dispatcher.Dispose();
                for (var index = _prefabObjects.Count - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(_prefabObjects[index]);
                }

                UnityEngine.Object.DestroyImmediate(_projectilesRoot);
                UnityEngine.Object.DestroyImmediate(_projectilePrefabObject);
                UnityEngine.Object.DestroyImmediate(_presentationPrefabObject);
            }
        }

        public abstract class TestActorViewBase : ActorViewBase
        {
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
            public override void PlayDamageFeedback(int amount, bool isFatal) { }
            public override void PlayDeathFeedback() { }
            protected override void OnInitialize() { }
            protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
            protected override void OnDispose() { }
            protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
        }

        public sealed class TestActorViewA : TestActorViewBase { }
        public sealed class TestActorViewB : TestActorViewBase { }

        private sealed class UnsupportedSkillDefinition : SkillDefinition
        {
            public UnsupportedSkillDefinition(SkillLevelDefinition level)
                : base(
                    "skill.unsupported",
                    "Unsupported",
                    SkillTargetRule.EnemyActor,
                    new[] { level })
            {
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

        private sealed class FakeResourceLoader : IResourceLoader
        {
            private readonly GameObject _prefab;
            private readonly SkillPresentationAsset _presentation;
            private readonly Texture2D _icon;

            public FakeResourceLoader(GameObject prefab)
            {
                _prefab = prefab;
                _presentation = ScriptableObject.CreateInstance<SkillPresentationAsset>();
                _icon = new Texture2D(1, 1);
            }

            public int ReleaseCount { get; private set; }

            public Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public TResource LoadResource<TResource>(string resourceId)
            {
                if (typeof(TResource) == typeof(SkillPresentationAsset))
                {
                    return (TResource)(object)_presentation;
                }

                if (typeof(TResource) == typeof(Texture2D))
                {
                    return (TResource)(object)_icon;
                }

                return (TResource)(object)_prefab;
            }

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token)
            {
                onResourceLoaded(LoadResource<TResource>(resourceId));
            }

            public Task<TResource> LoadResourceAsync<TResource>(
                string resourceId,
                CancellationToken token)
            {
                return Task.FromResult(LoadResource<TResource>(resourceId));
            }

            public Task<TComponent> LoadAndCreateAsync<TComponent, TParent>(
                string resourceId,
                TParent parent,
                CancellationToken token = default)
            {
                throw new NotSupportedException();
            }

            public void ReleaseResource<TResource>(TResource resource)
            {
                ReleaseCount++;
            }

            public void ReleaseAllResources() { }
            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_presentation);
                UnityEngine.Object.DestroyImmediate(_icon);
            }
        }
    }
}

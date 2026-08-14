using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using NUnit.Framework;
using TickHandler.UnityTickHandler;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.Team.Tests.PlayMode
{
    public sealed class CompanionCommandPlayModeTests
    {
        [UnityTest]
        public IEnumerator ExplicitAttack_DuringCooldown_ContinuesApproachingSkillRange()
        {
            var world = TestWorld.Create(includeEnemy: true);
            try
            {
                Assert.That(world.Team.TryOrderAttack(), Is.True);

                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(90));
                Assert.That(
                    world.Combat.GetSlotState(SkillSlot.Primary).CooldownRemaining,
                    Is.GreaterThan(0f));

                world.Enemy.SetMoveDirection(
                    new Vector3(0f, 0f, 5f) - world.Enemy.Position);
                var distanceBeforeChase = Vector3.Distance(
                    world.Companion.Position,
                    world.Enemy.Position);

                yield return null;

                Assert.That(
                    world.Combat.GetSlotState(SkillSlot.Primary).CooldownRemaining,
                    Is.GreaterThan(0f),
                    "Approach must not consume or bypass the active cooldown.");
                Assert.That(
                    Vector3.Distance(world.Companion.Position, world.Enemy.Position),
                    Is.LessThan(distanceBeforeChase),
                    "Explicit Attack must keep the companion inside the selected skill range.");
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitAttack_PrefersReadySlotAlreadyInRange()
        {
            var world = TestWorld.Create(
                includeEnemy: true,
                loadoutId: "loadout.test.multiattack");
            try
            {
                world.Enemy.SetMoveDirection(
                    new Vector3(0f, 0f, 4f) - world.Enemy.Position);
                var companionPosition = world.Companion.Position;

                Assert.That(world.Team.TryOrderAttack(), Is.True);

                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(80),
                    "The ready ranged slot must execute instead of chasing for Primary.");
                Assert.That(world.Companion.Position, Is.EqualTo(companionPosition));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Follow_DuringAutonomousHealPreCommit_RecallsThenResumesAutonomousHealing()
        {
            var world = TestWorld.Create(includeEnemy: false);
            try
            {
                world.Companion.SetMoveDirection(Vector3.forward * 5f);
                world.Leader.ApplyDamage(60);

                yield return null;

                Assert.That(world.Combat.CanCancelActiveUse, Is.True);
                Assert.That(world.Team.CanOrderFollow, Is.True,
                    "Follow must be available while an autonomous action can be cancelled.");

                world.Team.OrderFollow();

                Assert.That(world.Combat.IsBusy, Is.False);
                Assert.That(world.Combat.IsReady(SkillSlot.Active1), Is.True,
                    "A pre-commit cancellation must not consume cooldown.");

                var distanceBeforeRecall = Vector3.Distance(
                    world.Companion.Position,
                    Vector3.zero);
                yield return null;

                Assert.That(
                    Vector3.Distance(world.Companion.Position, Vector3.zero),
                    Is.LessThan(distanceBeforeRecall),
                    "Follow must recall the companion toward its formation position.");
                Assert.That(world.Leader.CurrentHealth, Is.EqualTo(40),
                    "Follow must not select autonomous healing while recall is active.");

                yield return new WaitForSeconds(0.7f);

                Assert.That(world.Leader.CurrentHealth, Is.EqualTo(65),
                    "Autonomous healing must resume after the one-shot recall completes.");
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitAttack_DuringAutonomousHealPreCommit_CancelsHealFirst()
        {
            var world = TestWorld.Create(includeEnemy: true);
            try
            {
                world.Leader.ApplyDamage(60);
                yield return null;

                Assert.That(world.Combat.CanCancelActiveUse, Is.True);
                Assert.That(world.Team.TryOrderAttack(), Is.True);
                Assert.That(world.Combat.IsReady(SkillSlot.Active1), Is.True,
                    "Explicit Attack must cancel the uncommitted heal without cooldown.");

                yield return null;

                Assert.That(world.Leader.CurrentHealth, Is.EqualTo(40));
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(90),
                    "The explicit command must replace autonomous healing with attack.");
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TacticalAnchor_FollowRecallsThenAutonomyReturnsToAnchor()
        {
            var world = TestWorld.Create(includeEnemy: false);
            try
            {
                var tacticalAnchor = new Vector3(5f, 0f, 0f);
                world.Team.SetTacticalAnchors(new[] { tacticalAnchor });

                yield return null;

                Assert.That(
                    Vector3.Distance(world.Companion.Position, tacticalAnchor),
                    Is.LessThan(5f));

                world.Team.OrderFollow();
                yield return null;
                var recalledPosition = world.Companion.Position;

                Assert.That(
                    Vector3.Distance(recalledPosition, Vector3.zero),
                    Is.LessThan(4f),
                    "FOLLOW must ignore the active tactical anchor.");

                yield return null;
                yield return null;

                Assert.That(
                    Vector3.Distance(world.Companion.Position, tacticalAnchor),
                    Is.LessThan(Vector3.Distance(recalledPosition, tacticalAnchor)),
                    "Autonomy must resume the authored anchor after recall completes.");
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        private sealed class TestWorld : IDisposable
        {
            private readonly GameObject _actorPrefabObject;
            private readonly GameObject _projectileRoot;
            private readonly GameObject _dispatcherObject;
            private readonly UnityTickHandler _tickHandler;
            private readonly SkillViewSet _views;
            private readonly SkillExecutionController _execution;

            private bool _isDisposed;

            private TestWorld(
                GameObject actorPrefabObject,
                GameObject projectileRoot,
                GameObject dispatcherObject,
                UnityTickHandler tickHandler,
                SkillViewSet views,
                SkillExecutionController execution,
                ActorInstance leader,
                ActorInstance companion,
                ActorInstance enemy,
                ActorCombatController combat,
                TeamController team)
            {
                _actorPrefabObject = actorPrefabObject;
                _projectileRoot = projectileRoot;
                _dispatcherObject = dispatcherObject;
                _tickHandler = tickHandler;
                _views = views;
                _execution = execution;
                Leader = leader;
                Companion = companion;
                Enemy = enemy;
                Combat = combat;
                Team = team;
            }

            public ActorInstance Leader { get; }
            public ActorInstance Companion { get; }
            public ActorInstance Enemy { get; }
            public ActorCombatController Combat { get; }
            public TeamController Team { get; }

            public static TestWorld Create(
                bool includeEnemy,
                string loadoutId = "loadout.test.companion")
            {
                var actorPrefabObject = new GameObject("CompanionCommandActorPrefab");
                actorPrefabObject.SetActive(false);
                var actorPrefab = actorPrefabObject.AddComponent<TestActorView>();
                var projectileRoot = new GameObject("CompanionCommandProjectiles");
                var dispatcherObject = new GameObject("CompanionCommandDispatcher");
                var tickHandler = new UnityTickHandler(
                    dispatcherObject.AddComponent<UnityDispatcherBehaviour>());
                var views = CreateViews();
                var execution = new SkillExecutionController(
                    views,
                    tickHandler,
                    projectileRoot.transform);

                var factory = new ActorFactory();
                var leader = CreateActor(
                    factory,
                    actorPrefab,
                    "actor.test.leader",
                    new Vector3(-2f, 0f, 0f));
                var companion = CreateActor(
                    factory,
                    actorPrefab,
                    "actor.test.companion",
                    Vector3.zero);
                var enemy = includeEnemy
                    ? CreateActor(
                        factory,
                        actorPrefab,
                        "actor.test.enemy",
                        new Vector3(0f, 0f, 1f))
                    : null;

                var combat = new ActorCombatController(
                    companion,
                    CreateCatalog(),
                    loadoutId,
                    execution);
                IReadOnlyList<ActorInstance> enemies = includeEnemy
                    ? new[] { enemy }
                    : Array.Empty<ActorInstance>();
                var team = new TeamController(
                    leader,
                    new[] { companion },
                    new[] { combat },
                    new[] { new Vector3(2f, 0f, 0f) },
                    enemies,
                    tickHandler,
                    new TeamControlSettings());
                team.Initialize();
                execution.Initialize();

                return new TestWorld(
                    actorPrefabObject,
                    projectileRoot,
                    dispatcherObject,
                    tickHandler,
                    views,
                    execution,
                    leader,
                    companion,
                    enemy,
                    combat,
                    team);
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                Team.Dispose();
                Combat.Dispose();
                _execution.Dispose();
                Enemy?.Dispose();
                Companion.Dispose();
                Leader.Dispose();
                _views.Dispose();
                _tickHandler.Dispose();
                Destroy(_actorPrefabObject);
                Destroy(_projectileRoot);
                Destroy(_dispatcherObject);
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

            private static SkillCatalog CreateCatalog()
            {
                return new SkillCatalog(
                    new[]
                    {
                        new DirectDamageSkillDefinitionConfig(
                            "skill.test.strike",
                            "Test Strike",
                            SkillTargetRule.EnemyActor,
                            new[]
                            {
                                new DirectDamageSkillLevelConfig(
                                    1,
                                    damage: 10,
                                    range: 1.5f,
                                    cooldown: 2f)
                            }),
                        new DirectDamageSkillDefinitionConfig(
                            "skill.test.ranged",
                            "Test Ranged",
                            SkillTargetRule.EnemyActor,
                            new[]
                            {
                                new DirectDamageSkillLevelConfig(
                                    1,
                                    damage: 20,
                                    range: 5.5f,
                                    cooldown: 2f)
                            })
                    },
                    Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                    new[]
                    {
                        new DirectHealSkillDefinitionConfig(
                            "skill.test.heal",
                            "Test Heal",
                            SkillTargetRule.AllyOrSelfActor,
                            new[]
                            {
                                new DirectHealSkillLevelConfig(
                                    1,
                                    healAmount: 25,
                                    range: 8f,
                                    cooldown: 2f,
                                    commitDelay: 0.5f)
                            })
                    },
                    new[]
                    {
                        new CombatLoadoutDefinitionConfig(
                            "loadout.test.companion",
                            new[]
                            {
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Primary,
                                    "skill.test.strike",
                                    1),
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Active1,
                                    "skill.test.heal",
                                    1)
                            }),
                        new CombatLoadoutDefinitionConfig(
                            "loadout.test.multiattack",
                            new[]
                            {
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Primary,
                                    "skill.test.strike",
                                    1),
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Active1,
                                    "skill.test.ranged",
                                    1)
                            })
                    });
            }

            private static SkillViewSet CreateViews()
            {
                return new SkillViewSet(
                    Array.Empty<SkillProjectileViewEntry>(),
                    new[]
                    {
                        Presentation("skill.test.strike"),
                        Presentation("skill.test.ranged"),
                        Presentation("skill.test.heal")
                    });
            }

            private static SkillPresentationViewEntry Presentation(string skillId)
            {
                return new SkillPresentationViewEntry(
                    skillId,
                    new SkillPresentationSequence(
                        Array.Empty<SkillActorAnimationCue>(),
                        Array.Empty<SkillVfxCue>()));
            }

            private static void Destroy(UnityEngine.Object instance)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
        }

        public sealed class TestActorView : ActorViewBase
        {
            public override Vector3 Position => transform.position;
            public override Vector3 Forward => transform.forward;
            public override bool IsOnNavMesh => true;
            public override Transform WeaponAnchor => null;
            public override Transform HitVfxAnchor => null;
            public override Transform OverheadAnchor => null;
            public override Transform SkillOriginAnchor => transform;

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

                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                return true;
            }

            public override void StopMovement()
            {
            }

            public override void PlayAttackFeedback()
            {
            }

            public override void PlayCastFeedback()
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

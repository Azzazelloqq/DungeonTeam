using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile;
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
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Primary),
                    Is.True);

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
                world.Controller.TryRequestSkill(SkillSlot.Primary);
                world.Input.Movement = Vector2.left;

                yield return null;

                world.Input.Movement = Vector2.zero;
                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
                Assert.That(
                    world.Controller.CanRequestSkill(SkillSlot.Primary),
                    Is.True);
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ManualMovement_CancelsPendingFireballCast()
        {
            var world = new TestWorld(useProjectileSkill: true);
            try
            {
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestSkill(SkillSlot.Primary);
                yield return null;

                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));
                world.Input.Movement = Vector2.left;
                yield return null;

                Assert.That(world.ActiveExecutionCount, Is.Zero);
                Assert.That(world.ActiveProjectileCount, Is.Zero);
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ManualMovement_OnCommitBoundary_CancelsBeforeExecutionTick()
        {
            var world = new TestWorld(
                useProjectileSkill: true,
                projectileCommitDelay: 0.3f,
                useManualTicks: true);
            try
            {
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestSkill(SkillSlot.Primary);
                world.Tick(0.2f);

                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));
                world.Input.Movement = Vector2.left;
                world.Tick(0.1f);

                Assert.That(world.ActiveExecutionCount, Is.Zero);
                Assert.That(world.ActiveProjectileCount, Is.Zero);
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RejectedAutoApproach_CancelsPendingSkill()
        {
            var world = new TestWorld();
            try
            {
                world.Hero.SkillOriginAnchor.GetComponent<TestActorView>().RejectMovement = true;
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestSkill(SkillSlot.Primary);

                yield return null;

                Assert.That(world.Controller.PendingSlot, Is.Null);
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Active1_WhenRequested_UsesActiveSlotDefinition()
        {
            var world = new TestWorld();
            try
            {
                world.Controller.TrySetTarget(world.Enemy);

                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.True);
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(30));
                Assert.That(world.Controller.SelectedSlot, Is.EqualTo(SkillSlot.Active1));
                Assert.That(world.Controller.PendingSlot, Is.Null);
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator EnemySkill_WithoutSelectedTarget_AcquiresNearestEnemy()
        {
            var world = new TestWorld();
            try
            {
                Assert.That(world.Controller.Target, Is.Null);
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Primary),
                    Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));

                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(40));
                Assert.That(world.FarEnemy.CurrentHealth, Is.EqualTo(60));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator AutomaticTargeting_WhileIdle_SelectsNearestVisibleEnemy()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.Enemy.SkillOriginAnchor.position = Vector3.right * 8f;
                world.FarEnemy.SkillOriginAnchor.position = Vector3.right * 8.01f;
                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.False);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualTarget_WhileInsideLossDistance_IsNotReplacedByNearerEnemy()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                Assert.That(world.Controller.TrySetTarget(world.FarEnemy), Is.True);
                world.FarEnemy.SkillOriginAnchor.position = Vector3.right * 10f;

                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualTarget_BeyondLossDistance_ReturnsToNearestAutomaticTarget()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                Assert.That(world.Controller.TrySetTarget(world.Enemy), Is.True);
                world.Enemy.SkillOriginAnchor.position = Vector3.right * 10.01f;

                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.False);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerSelection_OnEnemy_UsesManualTargetPriority()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                world.Input.QueueTargetSelection(world.Camera.WorldToScreenPoint(
                    world.FarEnemy.Position + Vector3.up));

                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerSelection_OnCurrentAutomaticTarget_SelectsItManually()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                world.Tick(0.1f);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
                Assert.That(world.Controller.IsTargetManuallySelected, Is.False);

                world.Input.QueueTargetSelection(world.Camera.WorldToScreenPoint(
                    world.Enemy.Position + Vector3.up));
                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerSelection_OnCurrentManualTarget_ReturnsToNearestAutomaticTarget()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                var farEnemyScreenPosition = world.Camera.WorldToScreenPoint(
                    world.FarEnemy.Position + Vector3.up);
                world.Input.QueueTargetSelection(farEnemyScreenPosition);
                world.Tick(0.1f);
                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));

                world.Input.QueueTargetSelection(farEnemyScreenPosition);
                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.False);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerSelection_OnCurrentManualTargetDuringPendingApproach_IsIgnored()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                var farEnemyScreenPosition = world.Camera.WorldToScreenPoint(
                    world.FarEnemy.Position + Vector3.up);
                world.Input.QueueTargetSelection(farEnemyScreenPosition);
                world.Tick(0.1f);
                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
                Assert.That(world.Controller.TryRequestSkill(SkillSlot.Primary), Is.True);
                Assert.That(world.Controller.PendingSlot, Is.EqualTo(SkillSlot.Primary));

                world.Input.QueueTargetSelection(farEnemyScreenPosition);
                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
                Assert.That(world.Controller.PendingSlot, Is.EqualTo(SkillSlot.Primary));
                Assert.That(world.ActiveExecutionCount, Is.Zero);
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerSelection_OnEmptySpace_ReturnsToAutomaticTargeting()
        {
            var world = new TestWorld(useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                Assert.That(world.Controller.TrySetTarget(world.FarEnemy), Is.True);
                world.Input.QueueTargetSelection(Vector2.zero);

                world.Tick(0.1f);

                Assert.That(world.Controller.IsTargetManuallySelected, Is.False);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticTargeting_DuringActiveUse_DoesNotRetarget()
        {
            var world = new TestWorld(
                useProjectileSkill: true,
                useManualTicks: true);
            try
            {
                world.Tick(0.1f);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
                Assert.That(world.Controller.TryRequestSkill(SkillSlot.Primary), Is.True);
                world.Tick(0.1f);
                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));

                world.FarEnemy.SkillOriginAnchor.position = Vector3.right;
                world.Tick(0.1f);

                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualTargetAndPointer_DuringActiveUse_DoNotRetargetOrCancel()
        {
            var world = new TestWorld(
                useProjectileSkill: true,
                useManualTicks: true);
            try
            {
                world.ConfigureCameraForTargetSelection();
                Assert.That(world.Controller.TrySetTarget(world.Enemy), Is.True);
                Assert.That(world.Controller.TryRequestSkill(SkillSlot.Primary), Is.True);
                world.Tick(0.1f);
                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));

                world.Enemy.SkillOriginAnchor.position = Vector3.right * 20f;
                world.Input.QueueTargetSelection(world.Camera.WorldToScreenPoint(
                    world.FarEnemy.Position + Vector3.up));
                world.Tick(0.1f);

                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));
                Assert.That(world.Controller.IsTargetManuallySelected, Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemySkill_WithExplicitTarget_DoesNotReplaceItWithNearerEnemy()
        {
            var world = new TestWorld();
            try
            {
                Assert.That(world.Controller.TrySetTarget(world.FarEnemy), Is.True);
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Primary),
                    Is.True);
                Assert.That(
                    world.Controller.CanRequestSkill(SkillSlot.Primary),
                    Is.False,
                    "A pending approach must keep the skill action busy.");

                yield return null;
                yield return null;

                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.FarEnemy.CurrentHealth, Is.EqualTo(40));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CanRequestSkill_WithAutoTarget_DoesNotMutatePersonalTarget()
        {
            var world = new TestWorld();
            try
            {
                Assert.That(
                    world.Controller.CanRequestSkill(SkillSlot.Primary),
                    Is.True);
                Assert.That(world.Controller.Target, Is.Null);
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemySkill_WhenNearestEnemyIsDead_AcquiresNextLivingEnemy()
        {
            var world = new TestWorld();
            try
            {
                world.Enemy.ApplyDamage(world.Enemy.CurrentHealth);

                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Primary),
                    Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));

                yield return null;
                yield return null;

                Assert.That(world.FarEnemy.CurrentHealth, Is.EqualTo(40));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator EnemySkill_WhenNearestEnemyIsBlocked_AcquiresVisibleEnemy()
        {
            var world = new TestWorld();
            GameObject obstacle = null;
            try
            {
                world.FarEnemy.SkillOriginAnchor.position = Vector3.forward * 8f;
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "HeroAutoTargetObstacle";
                obstacle.transform.position = new Vector3(2.5f, 1f, 0f);
                obstacle.transform.localScale = new Vector3(1f, 3f, 1f);
                Physics.SyncTransforms();

                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Primary),
                    Is.True);
                Assert.That(world.Controller.Target, Is.SameAs(world.FarEnemy));
            }
            finally
            {
                Object.Destroy(obstacle);
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SkillRequest_DuringCooldown_IsNotBuffered()
        {
            var world = new TestWorld();
            try
            {
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestSkill(SkillSlot.Active1);
                yield return null;

                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.False);
                yield return null;
                yield return null;

                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(30));
                Assert.That(world.Controller.PendingSlot, Is.Null);
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator SkillInput_WhileMoving_IsConsumedWithoutDelayedCast()
        {
            var world = new TestWorld();
            try
            {
                world.Input.Movement = Vector2.right;
                world.Input.RequestedSkillSlot = SkillSlot.Primary;

                yield return null;

                world.Input.Movement = Vector2.zero;
                yield return null;
                yield return null;

                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
                Assert.That(world.Controller.PendingSlot, Is.Null);
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.FarEnemy.CurrentHealth, Is.EqualTo(60));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator SkillRequest_WithoutEnemyInsideAutoRange_StillSelectsSlot()
        {
            var world = new TestWorld();
            try
            {
                world.Enemy.SkillOriginAnchor.position = Vector3.right * 8.01f;
                world.FarEnemy.SkillOriginAnchor.position = Vector3.right * 9f;
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.False);
                Assert.That(
                    world.Controller.SelectedSlot,
                    Is.EqualTo(SkillSlot.Active1));
                Assert.That(world.Controller.Target, Is.Null);
                Assert.That(world.Controller.PendingSlot, Is.Null);
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
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
                world.FarEnemy.ApplyDamage(world.FarEnemy.CurrentHealth);

                yield return null;

                Assert.That(world.Controller.Target, Is.Null);
                Assert.That(
                    world.Controller.CanRequestSkill(SkillSlot.Primary),
                    Is.False);
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Dispose_DuringSelfHealPreparing_DoesNotCommit()
        {
            var world = new TestWorld(
                useHealSkill: true,
                healCommitDelay: 0.35f,
                useManualTicks: true);
            try
            {
                world.Hero.ApplyDamage(50);
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.True);
                world.Tick(0.1f);
                Assert.That(world.ActiveExecutionCount, Is.EqualTo(1));

                world.Controller.Dispose();
                world.Tick(0.3f);

                Assert.That(world.ActiveExecutionCount, Is.Zero);
                Assert.That(world.Hero.CurrentHealth, Is.EqualTo(50));
            }
            finally
            {
                world.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DirectHeal_WithEnemySelected_UsesSelfFallback()
        {
            var world = new TestWorld(useHealSkill: true);
            try
            {
                world.Hero.ApplyDamage(50);
                world.Controller.TrySetTarget(world.Enemy);

                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.True);
                yield return null;

                Assert.That(world.Hero.CurrentHealth, Is.EqualTo(75));
                Assert.That(world.Enemy.CurrentHealth, Is.EqualTo(60));
                Assert.That(world.Controller.Target, Is.SameAs(world.Enemy));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DirectHeal_WithSelectedAlly_ApproachesAndHealsAlly()
        {
            var world = new TestWorld(useHealSkill: true);
            try
            {
                world.Ally.ApplyDamage(40);
                Assert.That(world.Controller.TrySetTarget(world.Ally), Is.True);
                Assert.That(
                    world.Controller.CanRequestSkill(SkillSlot.Primary),
                    Is.False);
                Assert.That(
                    world.Controller.TryRequestSkill(SkillSlot.Active1),
                    Is.True);

                yield return null;
                yield return null;

                Assert.That(world.Hero.Position.x, Is.GreaterThan(0f));
                Assert.That(world.Ally.CurrentHealth, Is.EqualTo(85));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DirectHeal_TargetFilledDuringApproach_CancelsPendingSkill()
        {
            var world = new TestWorld(useHealSkill: true);
            try
            {
                world.Ally.ApplyDamage(40);
                world.Controller.TrySetTarget(world.Ally);
                world.Controller.TryRequestSkill(SkillSlot.Active1);
                world.Ally.ApplyHeal(40);

                yield return null;

                Assert.That(world.Controller.PendingSlot, Is.Null);
                Assert.That(world.Hero.Position, Is.EqualTo(Vector3.zero));
                Assert.That(world.Ally.CurrentHealth, Is.EqualTo(100));
            }
            finally
            {
                world.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DirectHeal_SelfFallbackSurvivesSelectedEnemyDeath()
        {
            var world = new TestWorld(useHealSkill: true);
            try
            {
                world.Hero.ApplyDamage(50);
                world.Controller.TrySetTarget(world.Enemy);
                world.Controller.TryRequestSkill(SkillSlot.Active1);
                world.Enemy.ApplyDamage(world.Enemy.CurrentHealth);

                yield return null;

                Assert.That(world.Controller.Target, Is.Null);
                Assert.That(world.Hero.CurrentHealth, Is.EqualTo(75));
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
            private readonly ManualDispatcher _manualDispatcher;
            private readonly UnityTickHandler _tickHandler;
            private readonly SkillViewSet _skillViews;
            private readonly SkillExecutionController _skillExecution;
            private readonly ActorCombatController _combat;
            private readonly GameObject _projectilesRoot;
            private readonly GameObject _projectilePrefabObject;

            public TestWorld(
                bool useProjectileSkill = false,
                bool useHealSkill = false,
                float projectileCommitDelay = 0.35f,
                float healCommitDelay = 0f,
                bool useManualTicks = false)
            {
                if (useProjectileSkill && useHealSkill)
                    throw new System.ArgumentException("Test skill modes are mutually exclusive.");

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
                        movementSpeed: 4f),
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
                        movementSpeed: 3f),
                    new ActorSpawnRequest(
                        "Enemy",
                        new Vector3(5f, 0f, 0f),
                        Quaternion.identity));
                FarEnemy = factory.Create(
                    new ActorDefinition(
                        "actor.enemy.far",
                        actorPrefab),
                    new ActorRuntimeDefinition(
                        "actor.enemy.far",
                        level: 1,
                        maximumHealth: 60,
                        movementSpeed: 3f),
                    new ActorSpawnRequest(
                        "FarEnemy",
                        new Vector3(8f, 0f, 0f),
                        Quaternion.identity));
                Ally = factory.Create(
                    new ActorDefinition(
                        "actor.ally.test",
                        actorPrefab),
                    new ActorRuntimeDefinition(
                        "actor.ally.test",
                        level: 1,
                        maximumHealth: 100,
                        movementSpeed: 4f),
                    new ActorSpawnRequest(
                        "Ally",
                        new Vector3(7f, 0f, 0f),
                        Quaternion.identity));

                _cameraObject = new GameObject("HeroTestCamera");
                Camera = _cameraObject.AddComponent<Camera>();
                Camera.transform.rotation = Quaternion.identity;

                if (useManualTicks)
                {
                    _manualDispatcher = new ManualDispatcher();
                    _tickHandler = new UnityTickHandler(_manualDispatcher);
                }
                else
                {
                    _dispatcherObject = new GameObject("HeroTestDispatcher");
                    var dispatcher = _dispatcherObject.AddComponent<UnityDispatcherBehaviour>();
                    _tickHandler = new UnityTickHandler(dispatcher);
                }
                _projectilePrefabObject = new GameObject("HeroTestProjectilePrefab");
                _projectilePrefabObject.SetActive(false);
                var projectilePrefab = _projectilePrefabObject.AddComponent<SkillProjectileView>();
                _skillViews = new SkillViewSet(
                    useProjectileSkill
                        ? new[]
                        {
                            new SkillProjectileViewEntry("skill.fireball", projectilePrefab)
                        }
                        : System.Array.Empty<SkillProjectileViewEntry>(),
                    new[]
                    {
                        new SkillPresentationViewEntry("skill.fireball", EmptySequence()),
                        new SkillPresentationViewEntry("skill.test", EmptySequence()),
                        new SkillPresentationViewEntry("skill.active", EmptySequence()),
                        new SkillPresentationViewEntry("skill.heal", EmptySequence())
                    });
                _projectilesRoot = new GameObject("HeroTestProjectiles");
                _skillExecution = new SkillExecutionController(
                    _skillViews,
                    _tickHandler,
                    _projectilesRoot.transform);
                Input = new FakeHeroInput();
                _combat = new ActorCombatController(
                    Hero,
                    CreateSkillCatalog(
                        useProjectileSkill,
                        useHealSkill,
                        projectileCommitDelay,
                        healCommitDelay),
                    "loadout.test",
                    _skillExecution);
                Controller = new HeroController(
                    Hero,
                    new[] { Hero, Ally },
                    new[] { Enemy, FarEnemy },
                    Camera,
                    _tickHandler,
                    Input,
                    new HeroControlSettings(),
                    _combat);
                Controller.Initialize();
                _skillExecution.Initialize();
            }

            public ActorInstance Hero { get; }
            public ActorInstance Enemy { get; }
            public ActorInstance FarEnemy { get; }
            public ActorInstance Ally { get; }
            public FakeHeroInput Input { get; }
            public HeroController Controller { get; }
            public Camera Camera { get; }
            public int ActiveExecutionCount => _skillExecution.ActiveExecutionCount;
            public int ActiveProjectileCount => _skillExecution.ActiveProjectileCount;

            public void Tick(float deltaTime)
            {
                if (_manualDispatcher == null)
                    throw new System.InvalidOperationException("Test world does not use manual ticks.");

                _manualDispatcher.RaiseUpdate(deltaTime);
            }

            public void ConfigureCameraForTargetSelection()
            {
                Camera.transform.position = new Vector3(0f, 10f, -10f);
                Camera.transform.LookAt(Vector3.right * 5f + Vector3.up);
            }

            public void Dispose()
            {
                Controller.Dispose();
                _combat.Dispose();
                _skillExecution.Dispose();
                Hero.Dispose();
                Enemy.Dispose();
                FarEnemy.Dispose();
                Ally.Dispose();
                _skillViews.Dispose();
                _tickHandler.Dispose();
                _manualDispatcher?.Dispose();
                Object.Destroy(_actorPrefabObject);
                Object.Destroy(_cameraObject);
                Object.Destroy(_dispatcherObject);
                Object.Destroy(_projectilesRoot);
                Object.Destroy(_projectilePrefabObject);
            }

            private static SkillCatalog CreateSkillCatalog(
                bool useProjectileSkill,
                bool useHealSkill,
                float projectileCommitDelay,
                float healCommitDelay)
            {
                if (useProjectileSkill)
                {
                    return new SkillCatalog(
                        System.Array.Empty<DirectDamageSkillDefinitionConfig>(),
                        new[]
                        {
                            new ProjectileDamageSkillDefinitionConfig(
                                "skill.fireball",
                                "Fireball",
                                SkillTargetRule.EnemyActor,
                                new[]
                                {
                                    new ProjectileDamageSkillLevelConfig(
                                        1,
                                        14,
                                        6f,
                                        1.2f,
                                        8f,
                                        commitDelay: projectileCommitDelay,
                                        recoveryDuration: 0.25f)
                                })
                        },
                        System.Array.Empty<DirectHealSkillDefinitionConfig>(),
                        new[]
                        {
                            new CombatLoadoutDefinitionConfig(
                                "loadout.test",
                                new[]
                                {
                                    new CombatLoadoutSlotConfig(
                                        SkillSlot.Primary,
                                        "skill.fireball",
                                        1)
                                })
                        });
                }

                if (useHealSkill)
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
                                    new DirectDamageSkillLevelConfig(1, 20, 1.5f, 0.8f)
                                })
                        },
                        System.Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                        new[]
                        {
                            new DirectHealSkillDefinitionConfig(
                                "skill.heal",
                                "Heal",
                                SkillTargetRule.AllyOrSelfActor,
                                new[]
                                {
                                    new DirectHealSkillLevelConfig(
                                        1,
                                        25,
                                        5f,
                                        3f,
                                        healCommitDelay,
                                        healCommitDelay > 0f ? 0.25f : 0f)
                                })
                        },
                        new[]
                        {
                            new CombatLoadoutDefinitionConfig(
                                "loadout.test",
                                new[]
                                {
                                    new CombatLoadoutSlotConfig(
                                        SkillSlot.Primary,
                                        "skill.test",
                                        1),
                                    new CombatLoadoutSlotConfig(
                                        SkillSlot.Active1,
                                        "skill.heal",
                                        1)
                                })
                        });
                }

                return new SkillCatalog(
                    new[]
                    {
                        new DirectDamageSkillDefinitionConfig(
                            "skill.test",
                            "Test",
                            SkillTargetRule.EnemyActor,
                            new[] { new DirectDamageSkillLevelConfig(1, 20, 1.5f, 0.8f) }),
                        new DirectDamageSkillDefinitionConfig(
                            "skill.active",
                            "Active",
                            SkillTargetRule.EnemyActor,
                            new[] { new DirectDamageSkillLevelConfig(1, 30, 6f, 1.2f) })
                    },
                    System.Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                    System.Array.Empty<DirectHealSkillDefinitionConfig>(),
                    new[]
                    {
                        new CombatLoadoutDefinitionConfig(
                            "loadout.test",
                            new[]
                            {
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Primary,
                                    "skill.test",
                                    1),
                                new CombatLoadoutSlotConfig(
                                    SkillSlot.Active1,
                                    "skill.active",
                                    1)
                            })
                    });
            }

            private static SkillPresentationSequence EmptySequence()
            {
                return new SkillPresentationSequence(
                    System.Array.Empty<SkillActorAnimationCue>(),
                    System.Array.Empty<SkillVfxCue>());
            }
        }

        private sealed class FakeHeroInput : IHeroInput
        {
            public Vector2 Movement { get; set; }
            public SkillSlot? RequestedSkillSlot { get; set; }
            public Vector2? TargetSelection { get; set; }

            public void QueueTargetSelection(Vector2 screenPosition)
            {
                TargetSelection = screenPosition;
            }

            public bool TryConsumeTargetSelection(out Vector2 screenPosition)
            {
                if (!TargetSelection.HasValue)
                {
                    screenPosition = Vector2.zero;
                    return false;
                }

                screenPosition = TargetSelection.Value;
                TargetSelection = null;
                return true;
            }

            public bool TryConsumeSkillRequest(out SkillSlot slot)
            {
                if (!RequestedSkillSlot.HasValue)
                {
                    slot = default;
                    return false;
                }

                slot = RequestedSkillSlot.Value;
                RequestedSkillSlot = null;
                return true;
            }
        }

        private sealed class ManualDispatcher : TickHandler.IDispatcher
        {
            public event System.Action<float> OnUpdate;
            public event System.Action<float> OnLateUpdate;
            public event System.Action<float> OnFixedUpdate;
            public event System.Action<float> OnEndFrameUpdate;

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

        public sealed class TestActorView : ActorViewBase
        {
            public bool RejectMovement { get; set; }

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
                if (RejectMovement)
                    return false;

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

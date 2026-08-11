using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.Chests.Runtime;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using DungeonTeam.Gameplay.Team.Runtime;
using NUnit.Framework;
using TMPro;
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
            var rewardPickupPrefabObject = new GameObject("RewardPickupTestPrefab");
            var rewardPickupPrefab = rewardPickupPrefabObject.AddComponent<TestRewardPickupView>();
            var chestPrefabObject = new GameObject("ChestTestPrefab");
            var chestPrefab = chestPrefabObject.AddComponent<TestChestView>();
            var contextActionsParentObject = new GameObject(
                "ContextActionsTestParent",
                typeof(RectTransform));
            var contextActionsParent = contextActionsParentObject.GetComponent<RectTransform>();
            var cameraObject = new GameObject("TeamTestCamera");
            var worldCamera = cameraObject.AddComponent<Camera>();
            var dispatcherObject = new GameObject("TeamTestDispatcher");
            var dispatcher = dispatcherObject.AddComponent<UnityDispatcherBehaviour>();
            var tickHandler = new UnityTickHandler(dispatcher);
            var input = new FakeDungeonRunInput();
            var actorLoader = new FakeActorDefinitionLoader(actorPrefab);
            var root = new DungeonRunRoot(
                new FakeDungeonFactory(),
                CreateStartRequestAtLevel(seed: 42, level: 2),
                new DungeonRunBindings(contextActionsPrefab),
                actorLoader,
                CreateActorCatalog(),
                CreateCombatCatalog(),
                new FakeRewardPickupViewLoader(rewardPickupPrefab),
                new FakeChestViewLoader(chestPrefab),
                contextActionsParent,
                worldCamera,
                tickHandler,
                input,
                CreateRewardCatalog(),
                new TeamControlSettings(),
                new HeroControlSettings(),
                CreateEnemyBehaviorCatalog());
            DungeonRunResult? result = null;
            var finishedCount = 0;
            root.Finished += value =>
            {
                result = value;
                finishedCount++;
            };

            GameObject mapRoot = null;
            GameObject navigationRoot = null;
            GameObject actorsRoot = null;
            GameObject visionArea = null;
            GameObject contextActions = null;
            GameObject rewardsRoot = null;
            GameObject interestsRoot = null;
            GameObject chestObject = null;
            GameObject rewardPickup = null;
            try
            {
                yield return root.InitializeAsync(default).ToCoroutine();

                Assert.That(root.Leader.Level, Is.EqualTo(2));
                Assert.That(root.Leader.CurrentHealth, Is.EqualTo(120));

                mapRoot = GameObject.Find("DungeonTestMap");
                navigationRoot = GameObject.Find("DungeonRunNavigation");
                actorsRoot = GameObject.Find("DungeonRunActors");
                visionArea = GameObject.Find("EnemyVisionArea");
                contextActions = GameObject.Find("ContextActions");
                rewardsRoot = GameObject.Find("DungeonRunRewards");
                interestsRoot = GameObject.Find("DungeonRunInterests");
                chestObject = GameObject.Find("Chest_interest.test.chest");

                Assert.That(root.MapSnapshot.DungeonId, Is.EqualTo("dungeon.test"));
                Assert.That(root.Leader, Is.Not.Null);
                Assert.That(root.Companions, Has.Count.EqualTo(1));
                Assert.That(root.Enemies, Has.Count.EqualTo(2));
                Assert.That(root.Leader.ActorId, Is.EqualTo("actor.hero.leader"));
                Assert.That(root.Companions[0].ActorId, Is.EqualTo("actor.hero.companion"));
                Assert.That(root.Enemies[0].ActorId, Is.EqualTo("enemy.grunt"));
                Assert.That(root.Enemies[1].ActorId, Is.EqualTo("enemy.guard"));
                Assert.That(
                    actorLoader.LastRequestedIds,
                    Is.EquivalentTo(new[]
                    {
                        "actor.hero.leader",
                        "actor.hero.companion",
                        "enemy.grunt",
                        "enemy.guard"
                    }));
                Assert.That(root.Chests, Has.Count.EqualTo(1));
                Assert.That(root.Chests[0].RewardProfileId, Is.EqualTo("reward.common"));
                Assert.That(root.Enemies[0].IsAlive, Is.True);
                Assert.That(root.Enemies[1].IsAlive, Is.True);
                Assert.That(root.Leader.IsAlive, Is.True);
                Assert.That(input.IsEnabled, Is.True);
                Assert.That(mapRoot, Is.Not.Null);
                Assert.That(navigationRoot, Is.Not.Null);
                Assert.That(actorsRoot, Is.Not.Null);
                Assert.That(visionArea, Is.Not.Null);
                Assert.That(contextActions, Is.Not.Null);
                Assert.That(rewardsRoot, Is.Not.Null);
                Assert.That(interestsRoot, Is.Not.Null);
                Assert.That(chestObject, Is.Not.Null);
                Assert.That(root.CanExit, Is.False);
                Assert.That(FindButton(contextActions, "EXIT"), Is.Null);
                Assert.That(FindButton(contextActions, "OPEN"), Is.Null);
                Assert.That(FindButton(contextActions, "ATTACK"), Is.Null);
                Assert.That(FindButton(contextActions, "ORDER ATTACK"), Is.Not.Null);
                Assert.That(actorsRoot.transform.childCount, Is.EqualTo(4));
                Assert.That(actorsRoot.transform.Find("Enemy_enemy.test.a"), Is.Not.Null);
                Assert.That(actorsRoot.transform.Find("Enemy_enemy.test.b"), Is.Not.Null);

                var firstEnemyHealth = root.Enemies[0].CurrentHealth;
                var secondEnemyHealth = root.Enemies[1].CurrentHealth;
                var companionHealth = root.Companions[0].CurrentHealth;
                var firstEnemyPosition = root.Enemies[0].Position;

                input.PointerPosition = worldCamera.WorldToScreenPoint(
                    root.Enemies[1].Position + Vector3.up);
                input.TargetSelectionWasPressed = true;
                yield return null;
                input.TargetSelectionWasPressed = false;

                Assert.That(FindButton(contextActions, "ATTACK"), Is.Not.Null);
                var attackButton = FindButton(contextActions, "ORDER ATTACK");
                Assert.That(attackButton, Is.Not.Null);

                root.Enemies[0].ApplyDamage(1, root.Enemies[1]);
                yield return null;

                Assert.That(root.Enemies[0].Position, Is.EqualTo(firstEnemyPosition),
                    "Enemy must ignore an attacker that is not a team member.");
                Assert.That(root.Companions[0].CurrentHealth, Is.EqualTo(companionHealth));

                var healthBeforeTeamAttack = root.Enemies[0].CurrentHealth;
                attackButton.onClick.Invoke();
                yield return null;
                yield return null;

                Assert.That(root.Enemies[0].IsAlive, Is.True);
                Assert.That(root.Enemies[0].CurrentHealth, Is.LessThan(healthBeforeTeamAttack));
                Assert.That(root.Enemies[1].CurrentHealth, Is.EqualTo(secondEnemyHealth),
                    "Team command target must remain independent from HeroTarget.");
                Assert.That(root.Companions[0].CurrentHealth, Is.LessThan(companionHealth),
                    "Enemy attacked from outside its vision must retaliate against the attacker.");
                var directionToCompanion = root.Companions[0].Position - root.Enemies[0].Position;
                directionToCompanion.y = 0f;
                Assert.That(
                    Vector3.Dot(root.Enemies[0].Forward, directionToCompanion.normalized),
                    Is.GreaterThan(0.99f),
                    "Enemy and its vision cone must face the target before attacking.");

                input.PointerPosition = new Vector2(-1000f, -1000f);
                input.TargetSelectionWasPressed = true;
                yield return null;
                input.TargetSelectionWasPressed = false;
                Assert.That(FindButton(contextActions, "ATTACK"), Is.Null);

                root.Enemies[0].ApplyDamage(root.Enemies[0].CurrentHealth);
                yield return null;

                Assert.That(root.Enemies[0].IsAlive, Is.False);
                Assert.That(
                    actorsRoot.transform.Find("Enemy_enemy.test.a")
                        .GetComponent<TestActorView>()
                        .LastDamageWasFatal,
                    Is.True);
                Assert.That(root.Enemies[1].CurrentHealth, Is.EqualTo(secondEnemyHealth));
                Assert.That(root.KilledEnemyCount, Is.EqualTo(1));
                Assert.That(root.RewardPickupCount, Is.EqualTo(1));
                Assert.That(root.CanExit, Is.False);
                Assert.That(FindButton(contextActions, "EXIT"), Is.Null);

                root.Enemies[0].ApplyDamage(1);
                Assert.That(root.KilledEnemyCount, Is.EqualTo(1));
                Assert.That(root.RewardPickupCount, Is.EqualTo(1));

                rewardPickup = GameObject.Find("RewardPickup_reward.gold");
                Assert.That(rewardPickup, Is.Not.Null);
                Assert.That(FindButton(contextActions, "PICK UP"), Is.Null);

                root.Leader.SetMoveDirection(
                    root.Enemies[0].Position - root.Leader.Position);
                yield return null;

                var pickupButton = FindButton(contextActions, "PICK UP");
                Assert.That(pickupButton, Is.Not.Null);

                root.Leader.SetMoveDirection(Vector3.right * 3f);
                yield return null;
                Assert.That(FindButton(contextActions, "PICK UP"), Is.Null);

                root.Leader.SetMoveDirection(
                    root.Enemies[0].Position - root.Leader.Position);
                yield return null;
                pickupButton = FindButton(contextActions, "PICK UP");
                Assert.That(pickupButton, Is.Not.Null);
                pickupButton.onClick.Invoke();

                Assert.That(root.CollectedRewardCount, Is.EqualTo(1));
                Assert.That(
                    rewardPickup.GetComponent<TestRewardPickupView>().IsCollectedVisual,
                    Is.True);
                Assert.That(FindButton(contextActions, "PICK UP"), Is.Null);

                root.Leader.SetMoveDirection(
                    root.Chests[0].Position - root.Leader.Position);
                yield return null;

                var openButton = FindButton(contextActions, "OPEN");
                Assert.That(openButton, Is.Not.Null);
                openButton.onClick.Invoke();

                Assert.That(root.Chests[0].IsOpened, Is.True);
                Assert.That(
                    chestObject.GetComponent<TestChestView>().IsOpenedVisual,
                    Is.True);
                Assert.That(root.Chests[0].TryOpen(), Is.False);
                Assert.That(root.RewardPickupCount, Is.EqualTo(2));
                Assert.That(FindButton(contextActions, "OPEN"), Is.Null);
                Assert.That(FindButton(contextActions, "PICK UP"), Is.Not.Null);

                FindButton(contextActions, "PICK UP").onClick.Invoke();
                Assert.That(root.CollectedRewardCount, Is.EqualTo(2));

                yield return new WaitForSeconds(1.1f);

                Assert.That(root.Enemies[1].CurrentHealth, Is.EqualTo(secondEnemyHealth),
                    "Companion must not automatically chain to the next target.");

                root.Enemies[1].ApplyDamage(root.Enemies[1].CurrentHealth);
                yield return null;

                Assert.That(root.CanExit, Is.True);
                Assert.That(root.KilledEnemyCount, Is.EqualTo(2));
                Assert.That(FindButton(contextActions, "EXIT"), Is.Null);

                var exitPose = root.MapSnapshot.ExitPose;
                var exitPosition = new Vector3(
                    exitPose.PositionX,
                    exitPose.PositionY,
                    exitPose.PositionZ);
                root.Leader.SetMoveDirection(exitPosition - root.Leader.Position);
                yield return null;

                var exitButton = FindButton(contextActions, "EXIT");
                Assert.That(exitButton, Is.Not.Null);
                exitButton.onClick.Invoke();

                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(result.HasValue, Is.True);
                Assert.That(result.Value.Outcome, Is.EqualTo(DungeonRunOutcome.Completed));
                Assert.That(result.Value.KilledEnemies, Is.EqualTo(2));
                Assert.That(result.Value.CollectedRewardCount, Is.EqualTo(3));
                Assert.That(result.Value.CollectedRewards, Has.Count.EqualTo(2));
                Assert.That(result.Value.CollectedRewards[0].RewardId,
                    Is.EqualTo("reward.crystal"));
                Assert.That(result.Value.CollectedRewards[0].Amount, Is.EqualTo(1));
                Assert.That(result.Value.CollectedRewards[1].RewardId,
                    Is.EqualTo("reward.gold"));
                Assert.That(result.Value.CollectedRewards[1].Amount, Is.EqualTo(2));
                Assert.That(root.IsFinished, Is.True);

                exitButton.onClick.Invoke();
                Assert.That(finishedCount, Is.EqualTo(1));

                yield return null;

                Assert.That(input.IsDisposed, Is.True);
                Assert.That(contextActions.GetComponentsInChildren<Button>(), Is.Empty);
            }
            finally
            {
                root.Dispose();
                tickHandler.Dispose();
                Object.Destroy(actorPrefabObject);
                Object.Destroy(contextActionsPrefabObject);
                Object.Destroy(rewardPickupPrefabObject);
                Object.Destroy(chestPrefabObject);
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
            Assert.That(rewardsRoot == null, Is.True);
            Assert.That(interestsRoot == null, Is.True);
            Assert.That(chestObject == null, Is.True);
            Assert.That(rewardPickup == null, Is.True);
            Assert.That(input.IsDisposed, Is.True);
            Assert.That(actorLoader.LastLoadedSet.IsDisposed, Is.True);
        }

        [UnityTest]
        public IEnumerator Initialize_WithThreeSelectedCompanions_MaterializesAndTargetsWholeTeam()
        {
            var actorPrefabObject = new GameObject("MultiActorTestPrefab");
            actorPrefabObject.SetActive(false);
            var actorPrefab = actorPrefabObject.AddComponent<TestActorView>();
            var contextActionsPrefabObject = new GameObject(
                "MultiContextActionsTestPrefab",
                typeof(RectTransform));
            contextActionsPrefabObject.SetActive(false);
            var contextActionsPrefab = contextActionsPrefabObject.AddComponent<ContextActionsView>();
            var rewardPickupPrefabObject = new GameObject("MultiRewardPickupTestPrefab");
            var rewardPickupPrefab = rewardPickupPrefabObject.AddComponent<TestRewardPickupView>();
            var chestPrefabObject = new GameObject("MultiChestTestPrefab");
            var chestPrefab = chestPrefabObject.AddComponent<TestChestView>();
            var contextActionsParentObject = new GameObject(
                "MultiContextActionsTestParent",
                typeof(RectTransform));
            var cameraObject = new GameObject("MultiTeamTestCamera");
            var dispatcherObject = new GameObject("MultiTeamTestDispatcher");
            var tickHandler = new UnityTickHandler(
                dispatcherObject.AddComponent<UnityDispatcherBehaviour>());
            var actorLoader = new FakeActorDefinitionLoader(actorPrefab);
            var root = new DungeonRunRoot(
                new FakeDungeonFactory(),
                CreateStartRequest(
                    91,
                    "actor.hero.companion",
                    "actor.hero.rogue",
                    "actor.hero.wizard"),
                new DungeonRunBindings(contextActionsPrefab),
                actorLoader,
                CreateActorCatalog(),
                CreateCombatCatalog(),
                new FakeRewardPickupViewLoader(rewardPickupPrefab),
                new FakeChestViewLoader(chestPrefab),
                contextActionsParentObject.GetComponent<RectTransform>(),
                cameraObject.AddComponent<Camera>(),
                tickHandler,
                new FakeDungeonRunInput(),
                CreateRewardCatalog(),
                new TeamControlSettings(),
                new HeroControlSettings(),
                CreateEnemyBehaviorCatalog());

            try
            {
                yield return root.InitializeAsync(default).ToCoroutine();

                Assert.That(root.Heroes, Has.Count.EqualTo(4));
                Assert.That(root.Companions, Has.Count.EqualTo(3));
                Assert.That(root.Companions[0].ActorId, Is.EqualTo("actor.hero.companion"));
                Assert.That(root.Companions[1].ActorId, Is.EqualTo("actor.hero.rogue"));
                Assert.That(root.Companions[2].ActorId, Is.EqualTo("actor.hero.wizard"));
                Assert.That(
                    actorLoader.LastRequestedIds,
                    Is.EquivalentTo(new[]
                    {
                        "actor.hero.leader",
                        "actor.hero.companion",
                        "actor.hero.rogue",
                        "actor.hero.wizard",
                        "enemy.grunt",
                        "enemy.guard"
                    }));

                var thirdCompanion = root.Companions[2];
                root.Leader.SetMoveDirection(
                    root.Enemies[1].Position - root.Leader.Position);
                thirdCompanion.SetMoveDirection(
                    root.Enemies[1].Position + Vector3.right - thirdCompanion.Position);
                var healthBeforeAttack = thirdCompanion.CurrentHealth;
                root.Enemies[1].ApplyDamage(1, thirdCompanion);
                yield return null;

                Assert.That(
                    thirdCompanion.CurrentHealth,
                    Is.LessThan(healthBeforeAttack),
                    "Enemy AI must accept provoke from every selected team member.");
            }
            finally
            {
                root.Dispose();
                tickHandler.Dispose();
                Object.Destroy(actorPrefabObject);
                Object.Destroy(contextActionsPrefabObject);
                Object.Destroy(rewardPickupPrefabObject);
                Object.Destroy(chestPrefabObject);
                Object.Destroy(contextActionsParentObject);
                Object.Destroy(cameraObject);
                Object.Destroy(dispatcherObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Initialize_WithMeleeAndRangedBehaviors_UsesDistinctRangesAndDisposes()
        {
            var actorPrefabObject = new GameObject("BehaviorActorTestPrefab");
            actorPrefabObject.SetActive(false);
            var actorPrefab = actorPrefabObject.AddComponent<TestActorView>();
            var contextActionsPrefabObject = new GameObject(
                "BehaviorContextActionsTestPrefab",
                typeof(RectTransform));
            contextActionsPrefabObject.SetActive(false);
            var contextActionsPrefab = contextActionsPrefabObject.AddComponent<ContextActionsView>();
            var rewardPickupPrefabObject = new GameObject("BehaviorRewardPickupTestPrefab");
            var rewardPickupPrefab = rewardPickupPrefabObject.AddComponent<TestRewardPickupView>();
            var chestPrefabObject = new GameObject("BehaviorChestTestPrefab");
            var chestPrefab = chestPrefabObject.AddComponent<TestChestView>();
            var contextActionsParentObject = new GameObject(
                "BehaviorContextActionsTestParent",
                typeof(RectTransform));
            var cameraObject = new GameObject("BehaviorTestCamera");
            var dispatcherObject = new GameObject("BehaviorTestDispatcher");
            var tickHandler = new UnityTickHandler(
                dispatcherObject.AddComponent<UnityDispatcherBehaviour>());
            var input = new FakeDungeonRunInput();
            var root = new DungeonRunRoot(
                new FakeDungeonFactory(),
                CreateStartRequest(seed: 17),
                new DungeonRunBindings(contextActionsPrefab),
                new FakeActorDefinitionLoader(actorPrefab),
                CreateActorCatalog(),
                CreateCombatCatalog(),
                new FakeRewardPickupViewLoader(rewardPickupPrefab),
                new FakeChestViewLoader(chestPrefab),
                contextActionsParentObject.GetComponent<RectTransform>(),
                cameraObject.AddComponent<Camera>(),
                tickHandler,
                input,
                CreateRewardCatalog(),
                new TeamControlSettings(),
                new HeroControlSettings(),
                CreateEnemyBehaviorCatalog());

            try
            {
                yield return root.InitializeAsync(default).ToCoroutine();

                root.Companions[0].SetMoveDirection(
                    root.Leader.Position + new Vector3(100f, 0f, 100f) -
                    root.Companions[0].Position);
                var meleeTargetPosition = root.Leader.Position + new Vector3(-2f, 0f, -5f);
                var rangedTargetPosition = root.Leader.Position + new Vector3(2f, 0f, -5f);
                root.Enemies[0].SetMoveDirection(meleeTargetPosition - root.Enemies[0].Position);
                root.Enemies[1].SetMoveDirection(rangedTargetPosition - root.Enemies[1].Position);
                root.Enemies[0].TryFaceTowards(root.Leader.Position);
                root.Enemies[1].TryFaceTowards(root.Leader.Position);

                var teamHealthBeforeTick = root.Leader.CurrentHealth +
                                           root.Companions[0].CurrentHealth;
                var meleeDistanceBeforeTick = Vector3.Distance(
                    root.Enemies[0].Position,
                    root.Leader.Position);
                var rangedPositionBeforeTick = root.Enemies[1].Position;

                yield return null;

                Assert.That(
                    root.Leader.CurrentHealth + root.Companions[0].CurrentHealth,
                    Is.EqualTo(teamHealthBeforeTick - 10),
                    "Ranged profile must attack while the target is outside melee range.");
                Assert.That(
                    Vector3.Distance(root.Enemies[0].Position, root.Leader.Position),
                    Is.LessThan(meleeDistanceBeforeTick),
                    "Melee profile must continue closing the same distance.");
                Assert.That(
                    root.Enemies[1].Position,
                    Is.EqualTo(rangedPositionBeforeTick),
                    "Ranged profile must not close distance while already in attack range.");
                Assert.That(root.Enemies, Has.Count.EqualTo(2));
                Assert.That(GameObject.Find("EnemyVisionArea"), Is.Not.Null);

                root.Dispose();
                yield return null;

                Assert.That(input.IsDisposed, Is.True);
                Assert.That(GameObject.Find("EnemyVisionArea"), Is.Null);
            }
            finally
            {
                root.Dispose();
                tickHandler.Dispose();
                Object.Destroy(actorPrefabObject);
                Object.Destroy(contextActionsPrefabObject);
                Object.Destroy(rewardPickupPrefabObject);
                Object.Destroy(chestPrefabObject);
                Object.Destroy(contextActionsParentObject);
                Object.Destroy(cameraObject);
                Object.Destroy(dispatcherObject);
            }
        }

        [UnityTest]
        public IEnumerator LeaderDeath_WhileRunIsActive_FinishesAsDefeatedOnce()
        {
            var actorPrefabObject = new GameObject("DefeatActorTestPrefab");
            actorPrefabObject.SetActive(false);
            var actorPrefab = actorPrefabObject.AddComponent<TestActorView>();
            var contextActionsPrefabObject = new GameObject(
                "DefeatContextActionsTestPrefab",
                typeof(RectTransform));
            contextActionsPrefabObject.SetActive(false);
            var contextActionsPrefab = contextActionsPrefabObject.AddComponent<ContextActionsView>();
            var rewardPickupPrefabObject = new GameObject("DefeatRewardPickupTestPrefab");
            var rewardPickupPrefab = rewardPickupPrefabObject.AddComponent<TestRewardPickupView>();
            var chestPrefabObject = new GameObject("DefeatChestTestPrefab");
            var chestPrefab = chestPrefabObject.AddComponent<TestChestView>();
            var contextActionsParentObject = new GameObject(
                "DefeatContextActionsTestParent",
                typeof(RectTransform));
            var cameraObject = new GameObject("DefeatTestCamera");
            var dispatcherObject = new GameObject("DefeatTestDispatcher");
            var tickHandler = new UnityTickHandler(
                dispatcherObject.AddComponent<UnityDispatcherBehaviour>());
            var input = new FakeDungeonRunInput();
            var actorLoader = new FakeActorDefinitionLoader(actorPrefab);
            var root = new DungeonRunRoot(
                new FakeDungeonFactory(),
                CreateStartRequest(seed: 7),
                new DungeonRunBindings(contextActionsPrefab),
                actorLoader,
                CreateActorCatalog(),
                CreateCombatCatalog(),
                new FakeRewardPickupViewLoader(rewardPickupPrefab),
                new FakeChestViewLoader(chestPrefab),
                contextActionsParentObject.GetComponent<RectTransform>(),
                cameraObject.AddComponent<Camera>(),
                tickHandler,
                input,
                CreateRewardCatalog(),
                new TeamControlSettings(),
                new HeroControlSettings(),
                CreateEnemyBehaviorCatalog());

            DungeonRunResult? result = null;
            var finishedCount = 0;
            root.Finished += value =>
            {
                result = value;
                finishedCount++;
            };

            try
            {
                yield return root.InitializeAsync(default).ToCoroutine();

                root.Leader.ApplyDamage(root.Leader.CurrentHealth);
                root.Leader.ApplyDamage(1);

                Assert.That(finishedCount, Is.EqualTo(1));
                Assert.That(result.HasValue, Is.True);
                Assert.That(result.Value.Outcome, Is.EqualTo(DungeonRunOutcome.Defeated));
                Assert.That(result.Value.KilledEnemies, Is.Zero);
                Assert.That(result.Value.CollectedRewardCount, Is.Zero);
                Assert.That(root.IsFinished, Is.True);

                yield return null;

                Assert.That(input.IsDisposed, Is.True);
                var contextActions = GameObject.Find("ContextActions");
                Assert.That(contextActions.GetComponentsInChildren<Button>(), Is.Empty);
            }
            finally
            {
                root.Dispose();
                tickHandler.Dispose();
                Object.Destroy(actorPrefabObject);
                Object.Destroy(contextActionsPrefabObject);
                Object.Destroy(rewardPickupPrefabObject);
                Object.Destroy(chestPrefabObject);
                Object.Destroy(contextActionsParentObject);
                Object.Destroy(cameraObject);
                Object.Destroy(dispatcherObject);
            }
        }

        private static Button FindButton(GameObject contextActions, string label)
        {
            var buttons = contextActions.GetComponentsInChildren<Button>();
            for (var index = 0; index < buttons.Length; index++)
            {
                var text = buttons[index].GetComponentInChildren<TMP_Text>();
                if (text != null && text.text == label)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static RewardCatalog CreateRewardCatalog()
        {
            return new RewardCatalog(new[]
            {
                new RewardDefinition("reward.gold", "Gold"),
                new RewardDefinition("reward.crystal", "Crystal")
            });
        }

        private static DungeonRunStartRequest CreateStartRequest(
            int seed,
            params string[] companionActorIds)
        {
            if (companionActorIds.Length == 0)
            {
                companionActorIds = new[] { "actor.hero.companion" };
            }

            return new DungeonRunStartRequest(
                new DungeonBuildRequest(
                    "dungeon.test",
                    "scenario.test",
                    "normal",
                    seed),
                new DungeonRunTeamSelection(
                    "actor.hero.leader",
                    companionActorIds));
        }

        private static DungeonRunStartRequest CreateStartRequestAtLevel(
            int seed,
            int level)
        {
            return new DungeonRunStartRequest(
                new DungeonBuildRequest(
                    "dungeon.test",
                    "scenario.test",
                    "normal",
                    seed),
                new DungeonRunTeamSelection(
                    new DungeonRunActorSelection("actor.hero.leader", level),
                    new[]
                    {
                        new DungeonRunActorSelection("actor.hero.companion", 1)
                    }));
        }

        private static EnemyBehaviorCatalog CreateEnemyBehaviorCatalog()
        {
            return new EnemyBehaviorCatalog(new[]
            {
                new EnemyBehaviorDefinition(
                    "behavior.enemy.melee.basic",
                    new EnemyAiSettings(
                        viewDistance: 8f,
                        viewAngle: 90f,
                        targetLossDistance: 12f)),
                new EnemyBehaviorDefinition(
                    "behavior.enemy.ranged.basic",
                    new EnemyAiSettings(
                        viewDistance: 12f,
                        viewAngle: 100f,
                        targetLossDistance: 18f))
            });
        }

        private static ActorConfigCatalog CreateActorCatalog()
        {
            return new ActorConfigCatalog(new[]
            {
                ActorConfig("actor.hero.leader", 100, 4f, "loadout.hero.melee"),
                ActorConfig("actor.hero.companion", 80, 4f, "loadout.hero.melee"),
                ActorConfig("actor.hero.rogue", 80, 4f, "loadout.hero.melee"),
                ActorConfig("actor.hero.wizard", 80, 4f, "loadout.hero.ranged"),
                ActorConfig("enemy.grunt", 60, 3.5f, "loadout.enemy.melee"),
                ActorConfig("enemy.guard", 60, 3.5f, "loadout.enemy.ranged")
            });
        }

        private static ActorDefinitionConfig ActorConfig(
            string actorId,
            int health,
            float speed,
            string loadoutId)
        {
            return new ActorDefinitionConfig(
                actorId,
                actorId,
                loadoutId,
                new[]
                {
                    new ActorLevelDefinitionConfig(1, health, speed, 1),
                    new ActorLevelDefinitionConfig(2, health + health / 5, speed, 2)
                });
        }

        private static CombatCatalog CreateCombatCatalog()
        {
            return new CombatCatalog(
                new[]
                {
                    new AttackDefinitionConfig(
                        "attack.hero.melee",
                        "Hero Melee",
                        new[]
                        {
                            new AttackRankDefinitionConfig(1, 20, 1.5f, 0.8f),
                            new AttackRankDefinitionConfig(2, 24, 1.5f, 0.8f)
                        }),
                    new AttackDefinitionConfig(
                        "attack.hero.ranged",
                        "Hero Ranged",
                        new[]
                        {
                            new AttackRankDefinitionConfig(1, 14, 6f, 1.2f),
                            new AttackRankDefinitionConfig(2, 17, 6f, 1.2f)
                        }),
                    new AttackDefinitionConfig(
                        "attack.enemy.melee",
                        "Enemy Melee",
                        new[]
                        {
                            new AttackRankDefinitionConfig(1, 15, 1.5f, 1f),
                            new AttackRankDefinitionConfig(2, 18, 1.5f, 1f)
                        }),
                    new AttackDefinitionConfig(
                        "attack.enemy.ranged",
                        "Enemy Ranged",
                        new[]
                        {
                            new AttackRankDefinitionConfig(1, 10, 6f, 1.5f),
                            new AttackRankDefinitionConfig(2, 12, 6f, 1.5f)
                        })
                },
                new[]
                {
                    new CombatLoadoutDefinitionConfig("loadout.hero.melee", "attack.hero.melee"),
                    new CombatLoadoutDefinitionConfig("loadout.hero.ranged", "attack.hero.ranged"),
                    new CombatLoadoutDefinitionConfig("loadout.enemy.melee", "attack.enemy.melee"),
                    new CombatLoadoutDefinitionConfig("loadout.enemy.ranged", "attack.enemy.ranged")
                });
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
                        new EnemySpawnPlan(
                            "enemy.test.a",
                            "enemy.grunt",
                            "behavior.enemy.melee.basic",
                            "",
                            Pose(3f, 3f),
                            new[] { new DungeonRewardGrantPlan("reward.gold", 1) }),
                        new EnemySpawnPlan(
                            "enemy.test.b",
                            "enemy.guard",
                            "behavior.enemy.ranged.basic",
                            "",
                            Pose(-3f, 3f),
                            new[] { new DungeonRewardGrantPlan("reward.gold", 1) })
                    },
                    new[]
                    {
                        new InterestPointSpawnPlan(
                            "interest.test.chest",
                            "interest.chest.basic",
                            "reward.common",
                            Pose(5f, -3f),
                            new[] { new DungeonRewardGrantPlan("reward.crystal", 1) })
                    },
                    new ObjectiveSpawnPlan[0],
                    new[] { new DungeonRewardGrantPlan("reward.gold", 1) },
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

        private sealed class FakeActorDefinitionLoader : IActorDefinitionLoader
        {
            private readonly ActorViewBase _prefab;

            public FakeActorDefinitionLoader(ActorViewBase prefab)
            {
                _prefab = prefab;
            }

            public ActorDefinitionSet LastLoadedSet { get; private set; }

            public IReadOnlyList<string> LastRequestedIds { get; private set; }

            public UniTask<ActorDefinitionSet> LoadAsync(
                IReadOnlyList<string> actorIds,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var requestedIds = new string[actorIds.Count];
                for (var index = 0; index < actorIds.Count; index++)
                {
                    requestedIds[index] = actorIds[index];
                }

                LastRequestedIds = requestedIds;
                LastLoadedSet = new ActorDefinitionSet(new[]
                {
                    Definition("actor.hero.leader"),
                    Definition("actor.hero.companion"),
                    Definition("actor.hero.rogue"),
                    Definition("actor.hero.wizard"),
                    Definition("enemy.grunt"),
                    Definition("enemy.guard")
                });
                return UniTask.FromResult(LastLoadedSet);
            }

            private ActorDefinition Definition(
                string actorId)
            {
                return new ActorDefinition(
                    actorId,
                    _prefab);
            }
        }

        private sealed class FakeRewardPickupViewLoader : IRewardPickupViewLoader
        {
            private readonly RewardPickupViewBase _prefab;

            public FakeRewardPickupViewLoader(RewardPickupViewBase prefab)
            {
                _prefab = prefab;
            }

            public UniTask<RewardPickupViewSet> LoadAsync(
                IReadOnlyList<string> rewardIds,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var distinctIds = new List<string>(rewardIds.Count);
                var seenIds = new HashSet<string>();
                for (var index = 0; index < rewardIds.Count; index++)
                {
                    if (seenIds.Add(rewardIds[index]))
                    {
                        distinctIds.Add(rewardIds[index]);
                    }
                }

                var views = new RewardPickupViewBase[distinctIds.Count];
                for (var index = 0; index < views.Length; index++)
                {
                    views[index] = _prefab;
                }

                return UniTask.FromResult(new RewardPickupViewSet(distinctIds, views));
            }
        }

        private sealed class FakeChestViewLoader : IChestViewLoader
        {
            private readonly ChestViewBase _prefab;

            public FakeChestViewLoader(ChestViewBase prefab)
            {
                _prefab = prefab;
            }

            public bool Supports(string chestId)
            {
                return chestId == "interest.chest.basic";
            }

            public UniTask<ChestViewSet> LoadAsync(
                IReadOnlyList<string> chestIds,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var views = new ChestViewBase[chestIds.Count];
                for (var index = 0; index < views.Length; index++)
                {
                    views[index] = _prefab;
                }

                return UniTask.FromResult(new ChestViewSet(chestIds, views));
            }
        }

        public sealed class TestActorView : ActorViewBase
        {
            public bool LastDamageWasFatal { get; private set; }

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

                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
                LastDamageWasFatal = isFatal;
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

        public sealed class TestRewardPickupView : RewardPickupViewBase
        {
            public override Vector3 Position => transform.position;

            public bool IsCollectedVisual { get; private set; }

            public override void SetCollected(bool isCollected)
            {
                IsCollectedVisual = isCollected;
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

        public sealed class TestChestView : ChestViewBase
        {
            public override Vector3 Position => transform.position;

            public override Vector3 RewardPosition => transform.position;

            public bool IsOpenedVisual { get; private set; }

            public override void SetOpened(bool isOpened)
            {
                IsOpenedVisual = isOpened;
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

        private sealed class FakeDungeonRunInput : IDungeonRunInput
        {
            public Vector2 Movement => Vector2.zero;

            public float CameraYawDelta => 0f;

            public bool TargetSelectionWasPressed { get; set; }

            public Vector2 PointerPosition { get; set; }

            public bool BasicAttackWasPressed => false;

            public bool IsDisposed { get; private set; }

            public bool IsEnabled { get; private set; }

            public void Enable()
            {
                IsEnabled = true;
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}

using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonContentPlannerTests
    {
        private static readonly DungeonPose Pose =
            new DungeonPose(0f, 0f, 0f, 0f, 0f, 0f, 1f);

        [Test]
        public void Build_WithFixedPlacements_PreservesDesignerContent()
        {
            var planner = new DungeonContentPlanner();

            var plan = planner.Build(
                seed: 42,
                new[]
                {
                    new EnemyPlacement(
                        "enemy-fixed",
                        "enemy-fixed",
                        DungeonPlacementMode.Fixed,
                        null,
                        "enemy.designer",
                        "behavior.enemy.designer",
                        fixedActorLevel: 3,
                        encounterGroupId: "encounter.designer",
                        pose: Pose)
                },
                new[]
                {
                    new InterestPointPlacement(
                        "interest-fixed",
                        DungeonPlacementMode.Fixed,
                        null,
                        "interest.designer",
                        "reward.designer",
                        Pose)
                },
                Array.Empty<ObjectivePlacement>(),
                new DungeonScenario(
                    "scenario",
                    threatBudget: 0,
                    Array.Empty<EnemyCandidate>(),
                    Array.Empty<InterestPointRule>(),
                    Array.Empty<string>(),
                    Array.Empty<RequiredObjective>(),
                    new[]
                    {
                        new DungeonRewardProfile(
                            "reward.designer",
                            new[] { new DungeonRewardEntry("reward.gold", 2) })
                    },
                    Array.Empty<EnemyRewardRule>(),
                    completionRewardProfileId: null),
                CreateNormalDifficulty());

            Assert.That(plan.EnemySpawns, Has.Count.EqualTo(1));
            Assert.That(plan.EnemySpawns[0].EnemyId, Is.EqualTo("enemy.designer"));
            Assert.That(
                plan.EnemySpawns[0].BehaviorId,
                Is.EqualTo("behavior.enemy.designer"));
            Assert.That(plan.EnemySpawns[0].ActorLevel, Is.EqualTo(3));
            Assert.That(plan.EnemySpawns[0].EncounterGroupId, Is.EqualTo("encounter.designer"));
            Assert.That(plan.InterestPointSpawns, Has.Count.EqualTo(1));
            Assert.That(plan.InterestPointSpawns[0].InterestPointId, Is.EqualTo("interest.designer"));
            Assert.That(plan.InterestPointSpawns[0].RewardProfileId, Is.EqualTo("reward.designer"));
        }

        [Test]
        public void Build_WithOptionalPlacement_AddsOnlyExplicitlyEnabledContent()
        {
            var planner = new DungeonContentPlanner();
            var placements = new[]
            {
                new EnemyPlacement(
                    "enemy-disabled",
                    DungeonPlacementMode.OptionalFixed,
                    null,
                    "enemy.disabled",
                    "behavior.enemy.disabled",
                    null,
                    Pose),
                new EnemyPlacement(
                    "chunk0.enemy-enabled",
                    "enemy-enabled",
                    DungeonPlacementMode.OptionalFixed,
                    null,
                    "enemy.enabled",
                    "behavior.enemy.enabled",
                    null,
                    Pose)
            };
            var scenario = new DungeonScenario(
                "scenario",
                threatBudget: 0,
                Array.Empty<EnemyCandidate>(),
                Array.Empty<InterestPointRule>(),
                new[] { "enemy-enabled" },
                Array.Empty<RequiredObjective>());

            var plan = planner.Build(
                seed: 42,
                placements,
                Array.Empty<InterestPointPlacement>(),
                Array.Empty<ObjectivePlacement>(),
                scenario,
                CreateNormalDifficulty());

            Assert.That(plan.EnemySpawns, Has.Count.EqualTo(1));
            Assert.That(plan.EnemySpawns[0].EnemyId, Is.EqualTo("enemy.enabled"));
            Assert.That(
                plan.EnemySpawns[0].BehaviorId,
                Is.EqualTo("behavior.enemy.enabled"));
            Assert.That(plan.EnemySpawns[0].PlacementId, Is.EqualTo("chunk0.enemy-enabled"));
        }

        [Test]
        public void Build_WithEnemySlots_UsesCompatibilityAndThreatBudget()
        {
            var planner = new DungeonContentPlanner();
            var placements = new[]
            {
                new EnemyPlacement(
                    "slot-1",
                    DungeonPlacementMode.Slot,
                    "melee",
                    null,
                    null,
                    "encounter",
                    Pose),
                new EnemyPlacement(
                    "slot-2",
                    DungeonPlacementMode.Slot,
                    "melee",
                    null,
                    null,
                    "encounter",
                    Pose)
            };
            var scenario = new DungeonScenario(
                "scenario",
                threatBudget: 1,
                new[]
                {
                    new EnemyCandidate(
                        "enemy.ranged",
                        "behavior.enemy.ranged",
                        cost: 1,
                        weight: 1,
                        new[] { "ranged" }),
                    new EnemyCandidate(
                        "enemy.melee",
                        "behavior.enemy.melee",
                        actorLevel: 4,
                        cost: 1,
                        weight: 1,
                        allowedSlotTags: new[] { "melee" })
                },
                Array.Empty<InterestPointRule>(),
                Array.Empty<string>(),
                Array.Empty<RequiredObjective>());

            var plan = planner.Build(
                seed: 42,
                placements,
                Array.Empty<InterestPointPlacement>(),
                Array.Empty<ObjectivePlacement>(),
                scenario,
                CreateNormalDifficulty());

            Assert.That(plan.EnemySpawns, Has.Count.EqualTo(1));
            Assert.That(plan.EnemySpawns[0].EnemyId, Is.EqualTo("enemy.melee"));
            Assert.That(
                plan.EnemySpawns[0].BehaviorId,
                Is.EqualTo("behavior.enemy.melee"));
            Assert.That(plan.EnemySpawns[0].ActorLevel, Is.EqualTo(4));
        }

        [Test]
        public void Build_WithInterestRule_FillsCompatibleSlotAndAppliesDifficulty()
        {
            var planner = new DungeonContentPlanner();
            var placements = new[]
            {
                new InterestPointPlacement(
                    "interest-slot",
                    DungeonPlacementMode.Slot,
                    "loot",
                    null,
                    null,
                    Pose)
            };
            var scenario = new DungeonScenario(
                "scenario",
                threatBudget: 0,
                Array.Empty<EnemyCandidate>(),
                new[]
                {
                    new InterestPointRule(
                        "loot",
                        minCount: 1,
                        maxCount: 1,
                        new[]
                        {
                            new InterestPointCandidate(
                                "interest.chest",
                                weight: 1,
                                "reward.common")
                        })
                },
                Array.Empty<string>(),
                Array.Empty<RequiredObjective>(),
                new[]
                {
                    new DungeonRewardProfile(
                        "reward.common",
                        new[] { new DungeonRewardEntry("reward.gold", 2) })
                },
                Array.Empty<EnemyRewardRule>(),
                completionRewardProfileId: null);
            var difficulty = new DungeonDifficulty(
                "normal",
                threatBudgetMultiplier: 1f,
                interestPointCountMultiplier: 1f,
                rewardBudgetMultiplier: 1.5f);

            var plan = planner.Build(
                seed: 42,
                Array.Empty<EnemyPlacement>(),
                placements,
                Array.Empty<ObjectivePlacement>(),
                scenario,
                difficulty);

            Assert.That(plan.InterestPointSpawns, Has.Count.EqualTo(1));
            Assert.That(plan.InterestPointSpawns[0].InterestPointId, Is.EqualTo("interest.chest"));
            Assert.That(plan.InterestPointSpawns[0].RewardProfileId, Is.EqualTo("reward.common"));
            Assert.That(plan.InterestPointSpawns[0].Rewards[0].RewardId, Is.EqualTo("reward.gold"));
            Assert.That(plan.InterestPointSpawns[0].Rewards[0].Amount, Is.EqualTo(3));
            Assert.That(plan.RewardBudgetMultiplier, Is.EqualTo(1.5f));
        }

        [Test]
        public void Build_WithScenarioRewards_ResolvesEnemyAndCompletionRewards()
        {
            var planner = new DungeonContentPlanner();
            var scenario = new DungeonScenario(
                "scenario",
                threatBudget: 0,
                Array.Empty<EnemyCandidate>(),
                Array.Empty<InterestPointRule>(),
                Array.Empty<string>(),
                Array.Empty<RequiredObjective>(),
                new[]
                {
                    new DungeonRewardProfile(
                        "reward.enemy",
                        new[] { new DungeonRewardEntry("reward.gold", 2) }),
                    new DungeonRewardProfile(
                        "reward.completion",
                        new[] { new DungeonRewardEntry("reward.crystal", 1) })
                },
                new[] { new EnemyRewardRule("enemy.grunt", "reward.enemy") },
                "reward.completion");
            var difficulty = new DungeonDifficulty(
                "hard",
                threatBudgetMultiplier: 1f,
                interestPointCountMultiplier: 1f,
                rewardBudgetMultiplier: 2f);

            var plan = planner.Build(
                seed: 42,
                new[]
                {
                    new EnemyPlacement(
                        "enemy-fixed",
                        DungeonPlacementMode.Fixed,
                        null,
                        "enemy.grunt",
                        "behavior.enemy.melee",
                        null,
                        Pose)
                },
                Array.Empty<InterestPointPlacement>(),
                Array.Empty<ObjectivePlacement>(),
                scenario,
                difficulty);

            Assert.That(plan.EnemySpawns[0].Rewards[0].RewardId, Is.EqualTo("reward.gold"));
            Assert.That(plan.EnemySpawns[0].Rewards[0].Amount, Is.EqualTo(4));
            Assert.That(plan.CompletionRewards[0].RewardId, Is.EqualTo("reward.crystal"));
            Assert.That(plan.CompletionRewards[0].Amount, Is.EqualTo(2));
        }

        [TestCase(DungeonPlacementMode.Fixed)]
        [TestCase(DungeonPlacementMode.OptionalFixed)]
        public void CreateEnemyPlacement_WithFixedModeAndMissingBehaviorId_Throws(
            DungeonPlacementMode mode)
        {
            Assert.Throws<ArgumentException>(() => new EnemyPlacement(
                "enemy-fixed",
                mode,
                slotTag: null,
                fixedEnemyId: "enemy.grunt",
                fixedBehaviorId: null,
                encounterGroupId: null,
                Pose));
        }

        [Test]
        public void CreateEnemyCandidate_WithMissingBehaviorId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EnemyCandidate(
                "enemy.grunt",
                behaviorId: null,
                cost: 1,
                weight: 1,
                new[] { "enemy.common" }));
        }

        [Test]
        public void Build_WithMissingRequiredObjectiveSlot_Throws()
        {
            var planner = new DungeonContentPlanner();
            var scenario = new DungeonScenario(
                "scenario",
                threatBudget: 0,
                Array.Empty<EnemyCandidate>(),
                Array.Empty<InterestPointRule>(),
                Array.Empty<string>(),
                new[] { new RequiredObjective("objective.exit", "exit") });

            Assert.Throws<InvalidOperationException>(() => planner.Build(
                seed: 42,
                Array.Empty<EnemyPlacement>(),
                Array.Empty<InterestPointPlacement>(),
                Array.Empty<ObjectivePlacement>(),
                scenario,
                CreateNormalDifficulty()));
        }

        private static DungeonScenario CreateEmptyScenario()
        {
            return new DungeonScenario(
                "scenario",
                threatBudget: 0,
                Array.Empty<EnemyCandidate>(),
                Array.Empty<InterestPointRule>(),
                Array.Empty<string>(),
                Array.Empty<RequiredObjective>());
        }

        private static DungeonDifficulty CreateNormalDifficulty()
        {
            return new DungeonDifficulty(
                "normal",
                threatBudgetMultiplier: 1f,
                interestPointCountMultiplier: 1f,
                rewardBudgetMultiplier: 1f);
        }
    }
}

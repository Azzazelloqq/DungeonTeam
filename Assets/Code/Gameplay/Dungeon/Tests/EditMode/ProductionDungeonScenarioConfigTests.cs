using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class ProductionDungeonScenarioConfigTests
    {
        private const string DungeonConfigPath =
            "Assets/Content/Configuration/DungeonConfig.asset";
        private static readonly DungeonPose Pose =
            new DungeonPose(0f, 0f, 0f, 0f, 0f, 0f, 1f);

        [Test]
        public void ProductionConfig_MeleeScenario_ContainsOnlyWarriorCandidate()
        {
            var scenario = LoadScenario("scenario.melee");

            Assert.That(scenario.ThreatBudget, Is.EqualTo(1));
            Assert.That(scenario.EnemyCandidates, Has.Count.EqualTo(1));
            AssertCandidate(
                scenario.EnemyCandidates[0],
                "actor.skeleton.warrior",
                "behavior.enemy.melee.basic",
                "loadout.skeleton.warrior");
        }

        [Test]
        public void ProductionConfig_RangedScenario_ContainsOnlyMageCandidate()
        {
            var scenario = LoadScenario("scenario.ranged");

            Assert.That(scenario.ThreatBudget, Is.EqualTo(1));
            Assert.That(scenario.EnemyCandidates, Has.Count.EqualTo(1));
            AssertCandidate(
                scenario.EnemyCandidates[0],
                "actor.skeleton.mage",
                "behavior.enemy.ranged.basic",
                "loadout.skeleton.mage");
        }

        [Test]
        public void ProductionConfig_AreaScenario_ContainsOnlyAreaLoadoutCandidate()
        {
            var scenario = LoadScenario("scenario.area");

            Assert.That(scenario.ThreatBudget, Is.EqualTo(1));
            Assert.That(scenario.EnemyCandidates, Has.Count.EqualTo(1));
            AssertCandidate(
                scenario.EnemyCandidates[0],
                "actor.skeleton.mage",
                "behavior.enemy.ranged.basic",
                "loadout.skeleton.area");
        }

        [Test]
        public void ProductionConfig_MixedScenario_ContainsAllThreeFunctionalCandidates()
        {
            var scenario = LoadScenario("scenario.mixed");

            Assert.That(scenario.ThreatBudget, Is.EqualTo(3));
            Assert.That(scenario.EnemyCandidates, Has.Count.EqualTo(3));
            AssertCandidate(
                scenario.EnemyCandidates[0],
                "actor.skeleton.warrior",
                "behavior.enemy.melee.basic",
                "loadout.skeleton.warrior");
            AssertCandidate(
                scenario.EnemyCandidates[1],
                "actor.skeleton.mage",
                "behavior.enemy.ranged.basic",
                "loadout.skeleton.mage");
            AssertCandidate(
                scenario.EnemyCandidates[2],
                "actor.skeleton.mage",
                "behavior.enemy.ranged.basic",
                "loadout.skeleton.area");
        }

        [Test]
        public void ProductionConfig_MixedScenarioDefaultSeed_PlansAllThreeFunctions()
        {
            var scenario = LoadScenario("scenario.mixed");
            var planner = new DungeonContentPlanner();

            var plan = planner.Build(
                seed: 5,
                new[]
                {
                    EnemySlot("enemy.1"),
                    EnemySlot("enemy.2"),
                    EnemySlot("enemy.3")
                },
                new[]
                {
                    new InterestPointPlacement(
                        "interest.1",
                        DungeonPlacementMode.Slot,
                        "interest.common",
                        fixedInterestPointId: null,
                        fixedRewardProfileId: null,
                        Pose)
                },
                new[] { new ObjectivePlacement("objective.1", "objective.exit", Pose) },
                scenario,
                new DungeonDifficulty("normal", 1f, 1f, 1f));

            Assert.That(plan.EnemySpawns, Has.Count.EqualTo(3));
            Assert.That(
                plan.EnemySpawns[0].LoadoutId,
                Is.EqualTo("loadout.skeleton.warrior"));
            Assert.That(
                plan.EnemySpawns[1].LoadoutId,
                Is.EqualTo("loadout.skeleton.mage"));
            Assert.That(
                plan.EnemySpawns[2].LoadoutId,
                Is.EqualTo("loadout.skeleton.area"));
        }

        private static DungeonScenario LoadScenario(string scenarioId)
        {
            var config = AssetDatabase.LoadAssetAtPath<DungeonConfigPage>(DungeonConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing production config at {DungeonConfigPath}.");
            return config.RequireScenario(scenarioId).ToDomain();
        }

        private static void AssertCandidate(
            EnemyCandidate candidate,
            string enemyId,
            string behaviorId,
            string loadoutId)
        {
            Assert.That(candidate.EnemyId, Is.EqualTo(enemyId));
            Assert.That(candidate.BehaviorId, Is.EqualTo(behaviorId));
            Assert.That(candidate.LoadoutId, Is.EqualTo(loadoutId));
            Assert.That(candidate.ActorLevel, Is.EqualTo(1));
            Assert.That(candidate.Cost, Is.EqualTo(1));
            Assert.That(candidate.Weight, Is.EqualTo(1));
            Assert.That(candidate.AllowedSlotTags, Is.EqualTo(new[] { "enemy.common" }));
        }

        private static EnemyPlacement EnemySlot(string placementId)
        {
            return new EnemyPlacement(
                placementId,
                DungeonPlacementMode.Slot,
                "enemy.common",
                fixedEnemyId: null,
                fixedBehaviorId: null,
                fixedLoadoutId: null,
                encounterGroupId: null,
                Pose);
        }
    }
}

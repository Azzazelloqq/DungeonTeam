using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.Application
{
    public sealed class DungeonRunTeamSetupTests
    {
        private const string ActorConfigPath =
            "Assets/Content/Configuration/ActorConfig.asset";
        private const string DungeonRunConfigPath =
            "Assets/Content/Configuration/DungeonRunConfig.asset";
        private const string SkillConfigPath =
            "Assets/Content/Configuration/SkillConfig.asset";

        [Test]
        public void ProductionConfig_CreateTeamSetup_UsesDistinctFourHeroDefaultSelection()
        {
            var setup = CreateProductionTeamSetup();

            Assert.That(setup.DefaultSelection.Leader.ActorId, Is.EqualTo("actor.king"));
            Assert.That(setup.DefaultSelection.Leader.LoadoutId, Is.EqualTo("loadout.king"));
            Assert.That(setup.DefaultSelection.Companions, Has.Count.EqualTo(3));
            Assert.That(setup.DefaultSelection.Companions[0].ActorId, Is.EqualTo("actor.druid"));
            Assert.That(
                setup.DefaultSelection.Companions[0].LoadoutId,
                Is.EqualTo("loadout.druid.healer"));
            Assert.That(setup.DefaultSelection.Companions[1].ActorId, Is.EqualTo("actor.rogue"));
            Assert.That(
                setup.DefaultSelection.Companions[1].LoadoutId,
                Is.EqualTo("loadout.rogue"));
            Assert.That(setup.DefaultSelection.Companions[2].ActorId, Is.EqualTo("actor.wizard"));
            Assert.That(
                setup.DefaultSelection.Companions[2].LoadoutId,
                Is.EqualTo("loadout.wizard"));
        }

        [Test]
        public void ProductionConfig_CreateTeamSetup_DoesNotOfferEnemySkeletonLoadouts()
        {
            var setup = CreateProductionTeamSetup();

            for (var index = 0; index < setup.Members.Count; index++)
            {
                Assert.That(
                    setup.Members[index].SupportsLoadout("loadout.skeleton.mage"),
                    Is.False,
                    $"Playable actor '{setup.Members[index].ActorId}' must not offer the enemy mage loadout.");
                Assert.That(
                    setup.Members[index].SupportsLoadout("loadout.skeleton.warrior"),
                    Is.False,
                    $"Playable actor '{setup.Members[index].ActorId}' must not offer the enemy warrior loadout.");
            }
        }

        [Test]
        public void ProductionConfig_Rogue_IsMoreDurableThanRangedCompanions()
        {
            var actorCatalog = LoadProductionActorCatalog();

            for (var level = 1; level <= 2; level++)
            {
                var rogueHealth = actorCatalog.Resolve("actor.rogue", level).MaximumHealth;

                Assert.That(
                    rogueHealth,
                    Is.GreaterThan(actorCatalog.Resolve("actor.druid", level).MaximumHealth),
                    $"Rogue must remain the durability proxy at level {level}.");
                Assert.That(
                    rogueHealth,
                    Is.GreaterThan(actorCatalog.Resolve("actor.wizard", level).MaximumHealth),
                    $"Rogue must remain the durability proxy at level {level}.");
            }
        }

        [Test]
        public void CreateSelection_DuplicateActorId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DungeonRunTeamSelection(
                Selection("actor.king"),
                new[] { Selection("actor.druid"), Selection("actor.druid") }));
        }

        [Test]
        public void CreateSelection_LeaderAlsoCompanion_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DungeonRunTeamSelection(
                Selection("actor.king"),
                new[] { Selection("actor.king") }));
        }

        [Test]
        public void CreateSetup_DuplicateRosterActorId_Throws()
        {
            var members = new[] { Option("actor.king"), Option("actor.king") };

            Assert.Throws<ArgumentException>(() => new DungeonRunTeamSetup(
                members,
                1,
                2,
                new DungeonRunTeamSelection(Selection("actor.king"), Array.Empty<DungeonRunActorSelection>())));
        }

        [Test]
        public void RequireValid_UnknownActorId_Throws()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                Selection("actor.king"),
                new[] { Selection("actor.unknown") });

            Assert.Throws<ArgumentException>(() => setup.RequireValid(selection));
        }

        [Test]
        public void IsValid_ConfiguredTeamWithIndependentLoadouts_ReturnsTrue()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                Selection("actor.wizard", loadoutId: "loadout.melee"),
                new[]
                {
                    Selection("actor.king", loadoutId: "loadout.fireball"),
                    Selection("actor.rogue", loadoutId: "loadout.fireball")
                });

            Assert.That(setup.IsValid(selection), Is.True);
        }

        [Test]
        public void IsValid_UnavailableActorLevel_ReturnsFalse()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                Selection("actor.king", level: 3),
                new[] { Selection("actor.druid") });

            Assert.That(setup.IsValid(selection), Is.False);
        }

        [Test]
        public void IsValid_UnavailableLoadout_ReturnsFalse()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                Selection("actor.king", loadoutId: "loadout.unknown"),
                new[] { Selection("actor.druid") });

            Assert.That(setup.IsValid(selection), Is.False);
        }

        [Test]
        public void Selection_ActorLevelDoesNotChangeExplicitLoadout()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                Selection("actor.king", level: 2, loadoutId: "loadout.fireball"),
                new[]
                {
                    Selection("actor.druid", level: 1, loadoutId: "loadout.fireball")
                });

            setup.RequireValid(selection);

            Assert.That(selection.Leader.Level, Is.EqualTo(2));
            Assert.That(selection.Leader.LoadoutId, Is.EqualTo("loadout.fireball"));
            Assert.That(selection.Companions[0].Level, Is.EqualTo(1));
            Assert.That(
                selection.Companions[0].LoadoutId,
                Is.EqualTo(selection.Leader.LoadoutId));
        }

        private static DungeonRunTeamSetup CreateSetup()
        {
            return new DungeonRunTeamSetup(
                new[]
                {
                    Option("actor.king"),
                    Option("actor.druid"),
                    Option("actor.rogue"),
                    Option("actor.wizard")
                },
                2,
                4,
                new DungeonRunTeamSelection(
                    Selection("actor.king"),
                    new[] { Selection("actor.druid") }));
        }

        private static DungeonRunTeamSetup CreateProductionTeamSetup()
        {
            var runConfig = AssetDatabase.LoadAssetAtPath<DungeonRunConfigPage>(
                DungeonRunConfigPath);

            Assert.That(runConfig, Is.Not.Null, $"Missing production config at {DungeonRunConfigPath}.");

            return runConfig.CreateTeamSetup(
                LoadProductionActorCatalog(),
                LoadProductionSkillCatalog());
        }

        private static ActorConfigCatalog LoadProductionActorCatalog()
        {
            var config = AssetDatabase.LoadAssetAtPath<ActorConfigPage>(ActorConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing production config at {ActorConfigPath}.");
            return config.CreateCatalog();
        }

        private static SkillCatalog LoadProductionSkillCatalog()
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(SkillConfigPath);
            Assert.That(config, Is.Not.Null, $"Missing production config at {SkillConfigPath}.");
            return config.CreateCatalog();
        }

        private static DungeonRunTeamMemberOption Option(string actorId)
        {
            return new DungeonRunTeamMemberOption(
                actorId,
                actorId,
                new[] { 1, 2 },
                new[] { "loadout.melee", "loadout.fireball" });
        }

        private static DungeonRunActorSelection Selection(
            string actorId,
            int level = 1,
            string loadoutId = "loadout.melee")
        {
            return new DungeonRunActorSelection(actorId, level, loadoutId);
        }
    }
}

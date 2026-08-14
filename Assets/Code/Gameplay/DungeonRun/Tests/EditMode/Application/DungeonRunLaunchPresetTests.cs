using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.Application
{
    public sealed class DungeonRunLaunchPresetTests
    {
        private const string LaunchConfigPath =
            "Assets/Content/Configuration/DungeonRunLaunchConfig.asset";

        [Test]
        public void CreateCatalog_WithoutPresets_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DungeonRunLaunchPresetCatalog(
                Array.Empty<DungeonRunLaunchPreset>(),
                "product.default"));
        }

        [Test]
        public void CreateCatalog_WithDuplicatePresetId_Throws()
        {
            var presets = new[]
            {
                Preset("product.default", "Product Default", "dungeon.demo.authored"),
                Preset("product.default", "Duplicate", "dungeon.demo.chunked")
            };

            Assert.Throws<ArgumentException>(() => new DungeonRunLaunchPresetCatalog(
                presets,
                "product.default"));
        }

        [Test]
        public void CreateCatalog_WithUnknownDefaultPreset_Throws()
        {
            var presets = new[]
            {
                Preset("dev.authored", "Dev Authored", "dungeon.demo.authored")
            };

            Assert.Throws<ArgumentException>(() => new DungeonRunLaunchPresetCatalog(
                presets,
                "product.default"));
        }

        [Test]
        public void CreateRequest_WithoutSeedOverride_UsesPresetDefaultsAndSelectedTeam()
        {
            var team = Team();
            var catalog = new DungeonRunLaunchPresetCatalog(
                new[] { Preset("dev.chunked", "Dev Chunked", "dungeon.demo.chunked", 73) },
                "dev.chunked");

            var request = catalog.CreateRequest("dev.chunked", null, team);

            Assert.That(request.Dungeon.DungeonId, Is.EqualTo("dungeon.demo.chunked"));
            Assert.That(request.Dungeon.ScenarioId, Is.EqualTo("scenario.demo"));
            Assert.That(request.Dungeon.DifficultyId, Is.EqualTo("normal"));
            Assert.That(request.Dungeon.Seed, Is.EqualTo(73));
            Assert.That(request.Team, Is.SameAs(team));
        }

        [Test]
        public void CreateRequest_WithSeedOverride_UsesOverride()
        {
            var catalog = new DungeonRunLaunchPresetCatalog(
                new[] { Preset("dev.procedural", "Dev Procedural", "dungeon.demo.procedural", 73) },
                "dev.procedural");

            var request = catalog.CreateRequest("dev.procedural", 101, Team());

            Assert.That(request.Dungeon.Seed, Is.EqualTo(101));
        }

        [Test]
        public void ProductionConfig_CreateCatalog_ContainsClassicAndDeveloperPresets()
        {
            var config = AssetDatabase.LoadAssetAtPath<DungeonRunLaunchConfigPage>(
                LaunchConfigPath);

            Assert.That(config, Is.Not.Null, $"Missing production config at {LaunchConfigPath}.");

            var catalog = config.CreateCatalog();

            Assert.That(catalog.DefaultPreset.PresetId, Is.EqualTo("product.default"));
            Assert.That(catalog.Presets, Has.Count.EqualTo(9));
            Assert.That(catalog.Require("product.default").DungeonId, Is.EqualTo("dungeon.demo.authored"));
            Assert.That(catalog.Require("product.default").ScenarioId, Is.EqualTo("scenario.demo"));
            Assert.That(catalog.Require("dev.authored").DungeonId, Is.EqualTo("dungeon.demo.authored"));
            Assert.That(catalog.Require("dev.chunked").DungeonId, Is.EqualTo("dungeon.demo.chunked"));
            Assert.That(
                catalog.Require("dev.procedural").DungeonId,
                Is.EqualTo("dungeon.demo.procedural"));
            Assert.That(catalog.Require("dev.empty").ScenarioId, Is.EqualTo("scenario.empty"));
            AssertEnemyPreset(catalog, "dev.melee", "scenario.melee", 42);
            AssertEnemyPreset(catalog, "dev.ranged", "scenario.ranged", 42);
            AssertEnemyPreset(catalog, "dev.area", "scenario.area", 42);
            AssertEnemyPreset(catalog, "dev.mixed", "scenario.mixed", 5);
        }

        private static DungeonRunLaunchPreset Preset(
            string presetId,
            string displayName,
            string dungeonId,
            int defaultSeed = 42)
        {
            return new DungeonRunLaunchPreset(
                presetId,
                displayName,
                dungeonId,
                "scenario.demo",
                "normal",
                defaultSeed);
        }

        private static DungeonRunTeamSelection Team()
        {
            return new DungeonRunTeamSelection(
                new DungeonRunActorSelection("actor.king", 1, "loadout.king"),
                new[]
                {
                    new DungeonRunActorSelection("actor.druid", 1, "loadout.druid.healer")
                });
        }

        private static void AssertEnemyPreset(
            DungeonRunLaunchPresetCatalog catalog,
            string presetId,
            string scenarioId,
            int defaultSeed)
        {
            var preset = catalog.Require(presetId);

            Assert.That(preset.DungeonId, Is.EqualTo("dungeon.demo.procedural"));
            Assert.That(preset.ScenarioId, Is.EqualTo(scenarioId));
            Assert.That(preset.DefaultSeed, Is.EqualTo(defaultSeed));
        }
    }
}

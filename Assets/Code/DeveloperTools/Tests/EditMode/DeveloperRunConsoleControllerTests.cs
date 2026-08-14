using System;
using DungeonTeam.DeveloperTools;
using DungeonTeam.Gameplay.DungeonRun.Application;
using NUnit.Framework;

namespace DungeonTeam.DeveloperTools.Tests
{
    public sealed class DeveloperRunConsoleControllerTests
    {
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void Availability_UsesEditorOrDevelopmentBuild(
            bool isEditor,
            bool isDebugBuild,
            bool expected)
        {
            Assert.That(
                DeveloperRunConsoleAvailability.IsEnabled(isEditor, isDebugBuild),
                Is.EqualTo(expected));
        }

        [Test]
        public void Run_AfterCustomSelection_EmitsConfiguredProductionRequest()
        {
            DungeonRunStartRequest emitted = null;
            var controller = CreateController(request => emitted = request);

            controller.SelectPreset("dev.mixed");
            controller.Seed = 777;
            controller.SetLeader("actor.wizard");
            controller.SetActorIncluded("actor.king", false);
            controller.SetActorIncluded("actor.druid", false);
            controller.SetActorIncluded("actor.rogue", true);
            controller.SetActorLevel("actor.rogue", 2);
            controller.SetActorLoadout("actor.rogue", "loadout.rogue.alt");

            var accepted = controller.Run();

            Assert.That(accepted, Is.True);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.Dungeon.DungeonId, Is.EqualTo("dungeon.mixed"));
            Assert.That(emitted.Dungeon.Seed, Is.EqualTo(777));
            Assert.That(emitted.Team.Leader.ActorId, Is.EqualTo("actor.wizard"));
            Assert.That(emitted.Team.Companions, Has.Count.EqualTo(1));
            Assert.That(emitted.Team.Companions[0].ActorId, Is.EqualTo("actor.rogue"));
            Assert.That(emitted.Team.Companions[0].Level, Is.EqualTo(2));
            Assert.That(emitted.Team.Companions[0].LoadoutId, Is.EqualTo("loadout.rogue.alt"));
        }

        [Test]
        public void Run_WhenTeamIsOutsideConfiguredRange_DoesNotEmitAndPublishesError()
        {
            var emissionCount = 0;
            var controller = CreateController(_ => emissionCount++);

            controller.SetActorIncluded("actor.druid", false);

            var accepted = controller.Run();

            Assert.That(accepted, Is.False);
            Assert.That(emissionCount, Is.Zero);
            Assert.That(controller.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public void RandomizeSeed_UsesInjectedGenerator()
        {
            var controller = CreateController(_ => { }, seedGenerator: () => 9123);

            controller.RandomizeSeed();

            Assert.That(controller.Seed, Is.EqualTo(9123));
        }

        [Test]
        public void TrySetSeed_WhenTextIsNotAnInteger_RejectsValueAndPublishesError()
        {
            var controller = CreateController(_ => { });

            var accepted = controller.TrySetSeed("not-a-seed");

            Assert.That(accepted, Is.False);
            Assert.That(controller.Seed, Is.EqualTo(42));
            Assert.That(controller.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public void Stop_AlwaysEmitsStopRequest()
        {
            var stopped = false;
            var controller = CreateController(_ => { }, stopRequested: () => stopped = true);

            controller.Stop();

            Assert.That(stopped, Is.True);
        }

        [Test]
        public void Reset_RestoresCatalogAndTeamDefaults()
        {
            var controller = CreateController(_ => { });
            Assert.That(controller.Run(), Is.True);
            controller.SelectPreset("dev.mixed");
            controller.SetLeader("actor.wizard");
            controller.SetActorLevel("actor.rogue", 2);
            controller.SetActorLoadout("actor.rogue", "loadout.rogue.alt");

            controller.Reset();

            Assert.That(controller.SelectedPresetId, Is.EqualTo("product.default"));
            Assert.That(controller.Seed, Is.EqualTo(42));
            Assert.That(controller.LeaderActorId, Is.EqualTo("actor.king"));
            Assert.That(controller.IsActorIncluded("actor.druid"), Is.True);
            Assert.That(controller.IsActorIncluded("actor.rogue"), Is.False);
            Assert.That(controller.GetActorLevel("actor.rogue"), Is.EqualTo(1));
            Assert.That(controller.GetActorLoadout("actor.rogue"), Is.EqualTo("loadout.rogue"));
            Assert.That(controller.ErrorMessage, Is.Empty);
        }

        private static DeveloperRunConsoleController CreateController(
            Action<DungeonRunStartRequest> runRequested,
            Action stopRequested = null,
            Func<int> seedGenerator = null)
        {
            var king = new DungeonRunTeamMemberOption(
                "actor.king",
                "King",
                new[] { 1 },
                new[] { "loadout.king" });
            var druid = new DungeonRunTeamMemberOption(
                "actor.druid",
                "Druid",
                new[] { 1, 2 },
                new[] { "loadout.druid" });
            var rogue = new DungeonRunTeamMemberOption(
                "actor.rogue",
                "Rogue",
                new[] { 1, 2 },
                new[] { "loadout.rogue", "loadout.rogue.alt" });
            var wizard = new DungeonRunTeamMemberOption(
                "actor.wizard",
                "Wizard",
                new[] { 1, 2 },
                new[] { "loadout.wizard" });
            var defaultTeam = new DungeonRunTeamSelection(
                new DungeonRunActorSelection("actor.king", 1, "loadout.king"),
                new[]
                {
                    new DungeonRunActorSelection("actor.druid", 1, "loadout.druid")
                });
            var teamSetup = new DungeonRunTeamSetup(
                new[] { king, druid, rogue, wizard },
                minimumTeamSize: 2,
                maximumTeamSize: 4,
                defaultTeam);
            var presets = new DungeonRunLaunchPresetCatalog(
                new[]
                {
                    new DungeonRunLaunchPreset(
                        "product.default", "Default", "dungeon.default", "scenario.default",
                        "difficulty.normal", 42),
                    new DungeonRunLaunchPreset(
                        "dev.mixed", "Mixed", "dungeon.mixed", "scenario.mixed",
                        "difficulty.hard", 100)
                },
                "product.default");

            return new DeveloperRunConsoleController(
                presets,
                teamSetup,
                runRequested,
                stopRequested ?? (() => { }),
                seedGenerator ?? (() => 123));
        }
    }
}

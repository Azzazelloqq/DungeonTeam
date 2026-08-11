using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.Application
{
    public sealed class DungeonRunTeamSetupTests
    {
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

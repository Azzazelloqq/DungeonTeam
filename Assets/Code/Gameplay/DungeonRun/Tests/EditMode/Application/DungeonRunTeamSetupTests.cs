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
                "actor.king",
                new[] { "actor.druid", "actor.druid" }));
        }

        [Test]
        public void CreateSelection_LeaderAlsoCompanion_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DungeonRunTeamSelection(
                "actor.king",
                new[] { "actor.king" }));
        }

        [Test]
        public void CreateSetup_DuplicateRosterActorId_Throws()
        {
            var members = new[]
            {
                new DungeonRunTeamMemberOption("actor.king", "KING"),
                new DungeonRunTeamMemberOption("actor.king", "KING AGAIN")
            };

            Assert.Throws<ArgumentException>(() => new DungeonRunTeamSetup(
                members,
                1,
                2,
                new DungeonRunTeamSelection("actor.king", Array.Empty<string>())));
        }

        [Test]
        public void RequireValid_UnknownActorId_Throws()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                "actor.king",
                new[] { "actor.unknown" });

            Assert.Throws<ArgumentException>(() => setup.RequireValid(selection));
        }

        [TestCase(1)]
        [TestCase(5)]
        public void IsValid_TeamSizeOutsideConfiguredRange_ReturnsFalse(int memberCount)
        {
            var setup = CreateSetup();
            var actors = new[]
            {
                "actor.druid",
                "actor.rogue",
                "actor.wizard",
                "actor.extra"
            };
            var companions = new string[memberCount - 1];
            Array.Copy(actors, companions, companions.Length);
            var selection = new DungeonRunTeamSelection("actor.king", companions);

            Assert.That(setup.IsValid(selection), Is.False);
        }

        [Test]
        public void IsValid_ConfiguredTeam_ReturnsTrue()
        {
            var setup = CreateSetup();
            var selection = new DungeonRunTeamSelection(
                "actor.wizard",
                new[] { "actor.king", "actor.rogue" });

            Assert.That(setup.IsValid(selection), Is.True);
        }

        private static DungeonRunTeamSetup CreateSetup()
        {
            return new DungeonRunTeamSetup(
                new[]
                {
                    new DungeonRunTeamMemberOption("actor.king", "KING"),
                    new DungeonRunTeamMemberOption("actor.druid", "DRUID"),
                    new DungeonRunTeamMemberOption("actor.rogue", "ROGUE"),
                    new DungeonRunTeamMemberOption("actor.wizard", "WIZARD"),
                    new DungeonRunTeamMemberOption("actor.extra", "EXTRA")
                },
                2,
                4,
                new DungeonRunTeamSelection("actor.king", new[] { "actor.druid" }));
        }
    }
}

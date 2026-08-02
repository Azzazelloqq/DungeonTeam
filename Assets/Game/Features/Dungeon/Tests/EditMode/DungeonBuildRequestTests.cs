using System;
using DungeonTeam.Gameplay.Dungeon.Application;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Dungeon.Tests.EditMode
{
    public sealed class DungeonBuildRequestTests
    {
        [TestCase(null, "scenario", "normal")]
        [TestCase("dungeon", null, "normal")]
        [TestCase("dungeon", "scenario", null)]
        [TestCase(" ", "scenario", "normal")]
        [TestCase("dungeon", " ", "normal")]
        [TestCase("dungeon", "scenario", " ")]
        public void Create_WithMissingRequiredId_Throws(
            string dungeonId,
            string scenarioId,
            string difficultyId)
        {
            Assert.Throws<ArgumentException>(() =>
                new DungeonBuildRequest(dungeonId, scenarioId, difficultyId, seed: 42));
        }
    }
}

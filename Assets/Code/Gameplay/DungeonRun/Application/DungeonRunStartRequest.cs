using System;
using DungeonTeam.Gameplay.Dungeon.Application;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public sealed class DungeonRunStartRequest
    {
        public DungeonRunStartRequest(
            DungeonBuildRequest dungeon,
            DungeonRunTeamSelection team)
        {
            if (string.IsNullOrWhiteSpace(dungeon.DungeonId) ||
                string.IsNullOrWhiteSpace(dungeon.ScenarioId) ||
                string.IsNullOrWhiteSpace(dungeon.DifficultyId))
            {
                throw new ArgumentException(
                    "Dungeon build request must be initialized.",
                    nameof(dungeon));
            }

            Dungeon = dungeon;
            Team = team ?? throw new ArgumentNullException(nameof(team));
        }

        public DungeonBuildRequest Dungeon { get; }

        public DungeonRunTeamSelection Team { get; }
    }
}

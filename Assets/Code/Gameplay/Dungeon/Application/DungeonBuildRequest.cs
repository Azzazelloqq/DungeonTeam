using System;

namespace DungeonTeam.Gameplay.Dungeon.Application
{
    public readonly struct DungeonBuildRequest
    {
        public DungeonBuildRequest(
            string dungeonId,
            string scenarioId,
            string difficultyId,
            int seed)
        {
            DungeonId = RequireId(dungeonId, nameof(dungeonId));
            ScenarioId = RequireId(scenarioId, nameof(scenarioId));
            DifficultyId = RequireId(difficultyId, nameof(difficultyId));
            Seed = seed;
        }

        public string DungeonId { get; }
        public string ScenarioId { get; }
        public string DifficultyId { get; }
        public int Seed { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value;
        }
    }
}

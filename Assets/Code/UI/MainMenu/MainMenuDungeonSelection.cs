using System;
using DungeonTeam.Gameplay.DungeonRun.Application;

namespace Code.UI.MainMenu
{
    public readonly struct MainMenuDungeonOption
    {
        public MainMenuDungeonOption(string displayName, string dungeonId)
        {
            DisplayName = RequireValue(displayName, nameof(displayName));
            DungeonId = RequireValue(dungeonId, nameof(dungeonId));
        }

        public string DisplayName { get; }

        public string DungeonId { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public readonly struct MainMenuPlayRequest
    {
        public MainMenuPlayRequest(
            string dungeonId,
            int seed,
            DungeonRunTeamSelection team)
        {
            if (string.IsNullOrWhiteSpace(dungeonId))
            {
                throw new ArgumentException("Dungeon ID cannot be empty.", nameof(dungeonId));
            }

            DungeonId = dungeonId;
            Seed = seed;
            Team = team ?? throw new ArgumentNullException(nameof(team));
        }

        public string DungeonId { get; }

        public int Seed { get; }

        public DungeonRunTeamSelection Team { get; }
    }
}

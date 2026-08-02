using System;

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
        public MainMenuPlayRequest(string dungeonId, int seed)
        {
            if (string.IsNullOrWhiteSpace(dungeonId))
            {
                throw new ArgumentException("Dungeon ID cannot be empty.", nameof(dungeonId));
            }

            DungeonId = dungeonId;
            Seed = seed;
        }

        public string DungeonId { get; }

        public int Seed { get; }
    }
}

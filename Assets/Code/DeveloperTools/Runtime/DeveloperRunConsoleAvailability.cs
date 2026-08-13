namespace DungeonTeam.DeveloperTools
{
    public static class DeveloperRunConsoleAvailability
    {
        public static bool IsEnabled(bool isEditor, bool isDebugBuild)
        {
            return isEditor || isDebugBuild;
        }
    }
}

using System;

namespace DungeonTeam.Gameplay.Dungeon.Application
{
    public enum DungeonBuildFailureReason
    {
        InvalidConfig = 0,
        MissingAsset = 1,
        InvalidAuthoring = 2
    }

    public sealed class DungeonBuildException : Exception
    {
        public DungeonBuildException(
            DungeonBuildFailureReason reason,
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
            Reason = reason;
        }

        public DungeonBuildFailureReason Reason { get; }
    }
}

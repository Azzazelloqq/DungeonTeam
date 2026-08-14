using System;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonMapSnapshot
    {
        public DungeonMapSnapshot(
            string dungeonId,
            int seed,
            DungeonPose entryPose,
            DungeonPose exitPose)
            : this(
                dungeonId,
                seed,
                entryPose,
                exitPose,
                DungeonSpatialLayout.Empty)
        {
        }

        public DungeonMapSnapshot(
            string dungeonId,
            int seed,
            DungeonPose entryPose,
            DungeonPose exitPose,
            DungeonSpatialLayout spatialLayout)
        {
            if (string.IsNullOrWhiteSpace(dungeonId))
            {
                throw new ArgumentException("Dungeon ID cannot be empty.", nameof(dungeonId));
            }

            DungeonId = dungeonId;
            Seed = seed;
            EntryPose = entryPose;
            ExitPose = exitPose;
            SpatialLayout = spatialLayout ??
                throw new ArgumentNullException(nameof(spatialLayout));
        }

        public string DungeonId { get; }
        public int Seed { get; }
        public DungeonPose EntryPose { get; }
        public DungeonPose ExitPose { get; }
        public DungeonSpatialLayout SpatialLayout { get; }
    }
}

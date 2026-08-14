using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera
{
    internal readonly struct DungeonCameraPose
    {
        public DungeonCameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}

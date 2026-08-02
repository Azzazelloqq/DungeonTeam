using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal static class DungeonPoseConversion
    {
        public static Vector3 ToPosition(DungeonPose pose)
        {
            return new Vector3(pose.PositionX, pose.PositionY, pose.PositionZ);
        }

        public static Quaternion ToRotation(DungeonPose pose)
        {
            return new Quaternion(
                pose.RotationX,
                pose.RotationY,
                pose.RotationZ,
                pose.RotationW);
        }
    }
}

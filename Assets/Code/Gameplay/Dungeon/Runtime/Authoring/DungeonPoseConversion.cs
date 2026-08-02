using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonPoseConversion
    {
        public static DungeonPose ToDungeonPose(this Transform transform)
        {
            var position = transform.position;
            var rotation = transform.rotation;
            return new DungeonPose(
                position.x,
                position.y,
                position.z,
                rotation.x,
                rotation.y,
                rotation.z,
                rotation.w);
        }
    }
}

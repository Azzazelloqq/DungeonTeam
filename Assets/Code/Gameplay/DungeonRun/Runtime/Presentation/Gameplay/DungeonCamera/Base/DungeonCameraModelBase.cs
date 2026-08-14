using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base
{
    internal abstract class DungeonCameraModelBase : Model
    {
        public abstract DungeonCameraPose Snap(Vector3 leaderPosition);

        public abstract DungeonCameraPose Advance(Vector3 leaderPosition, float deltaTime);
    }
}

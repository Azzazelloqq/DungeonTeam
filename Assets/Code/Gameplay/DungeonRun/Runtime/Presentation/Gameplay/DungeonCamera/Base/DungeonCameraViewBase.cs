using MVP;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base
{
    internal abstract class DungeonCameraViewBase : View<DungeonCameraPresenterBase>
    {
        public abstract void ApplyPose(DungeonCameraPose pose);
    }
}

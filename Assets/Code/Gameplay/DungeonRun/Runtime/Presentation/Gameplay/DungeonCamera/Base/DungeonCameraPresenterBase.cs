using MVP;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base
{
    internal abstract class DungeonCameraPresenterBase :
        Presenter<DungeonCameraViewBase, DungeonCameraModelBase>
    {
        protected DungeonCameraPresenterBase(
            DungeonCameraViewBase view,
            DungeonCameraModelBase model)
            : base(view, model)
        {
        }
    }
}

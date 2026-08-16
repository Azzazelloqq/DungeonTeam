using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base
{
    public abstract class AmbientNpcPresenterBase : Presenter<AmbientNpcViewBase, AmbientNpcModelBase>
    {
        protected AmbientNpcPresenterBase(AmbientNpcViewBase view, AmbientNpcModelBase model) : base(view, model)
        {
        }

        public abstract void Tick(float deltaTime);
        public abstract void PauseAndFace(Vector3 playerPosition);
        public abstract void ResumeRoutine();
        public abstract void FaceVignetteTarget(Vector3 target, float deltaTime);
        public abstract void SetVignetteActivity(bool isSpeaking);
    }
}

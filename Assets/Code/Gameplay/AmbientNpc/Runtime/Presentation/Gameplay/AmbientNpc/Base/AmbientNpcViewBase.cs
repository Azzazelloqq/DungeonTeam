using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base
{
    public abstract class AmbientNpcViewBase : ViewMonoBehaviour<AmbientNpcPresenterBase>
    {
        public abstract string NpcId { get; }
        public abstract Transform BodyTransform { get; }
        public abstract Transform InteractionAnchor { get; }
        public abstract Transform ActivityAnchor { get; }
        public abstract Transform[] RouteAnchors { get; }

        public abstract void ValidateBindings();
        public abstract bool MoveTowards(Vector3 target, float speed, float deltaTime);
        public abstract bool FaceTowards(Vector3 target, float turnSpeed, float deltaTime);
        public abstract void SetActivityPose(AmbientNpcActivityPose pose);
    }
}

using DungeonTeam.Gameplay.AmbientNpc.Application;
using MVP;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base
{
    public abstract class AmbientNpcModelBase : Model
    {
        public abstract AmbientNpcRoutineState State { get; }
        public abstract bool IsPaused { get; }
        public abstract int RouteAnchorIndex { get; }
        public abstract float StateElapsed { get; }

        public abstract void SetState(AmbientNpcRoutineState state);
        public abstract void SetPaused(bool isPaused);
        public abstract void SetRouteAnchorIndex(int index);
        public abstract void AdvanceElapsed(float deltaTime);
        public abstract void ResetElapsed();
    }
}

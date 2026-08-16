using System;

namespace DungeonTeam.Gameplay.AmbientNpc.Application
{
    public enum AmbientNpcRoutineState
    {
        Idle,
        MoveToAnchor,
        FaceAnchor,
        Activity
    }

    public sealed class AmbientNpcRoutineStateMachine
    {
        public AmbientNpcRoutineState Current { get; private set; } = AmbientNpcRoutineState.Idle;

        public void Advance(bool hasRoute)
        {
            Current = Current switch
            {
                AmbientNpcRoutineState.Idle => hasRoute
                    ? AmbientNpcRoutineState.MoveToAnchor
                    : AmbientNpcRoutineState.Activity,
                AmbientNpcRoutineState.MoveToAnchor => AmbientNpcRoutineState.FaceAnchor,
                AmbientNpcRoutineState.FaceAnchor => AmbientNpcRoutineState.Activity,
                AmbientNpcRoutineState.Activity => AmbientNpcRoutineState.Idle,
                _ => throw new InvalidOperationException($"Unsupported routine state '{Current}'.")
            };
        }

        public void Reset()
        {
            Current = AmbientNpcRoutineState.Idle;
        }
    }
}

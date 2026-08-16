using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc
{
    public sealed class AmbientNpcModel : AmbientNpcModelBase
    {
        private AmbientNpcRoutineState _state = AmbientNpcRoutineState.Idle;
        private bool _isPaused;
        private int _routeAnchorIndex;
        private float _stateElapsed;

        public override AmbientNpcRoutineState State => _state;
        public override bool IsPaused => _isPaused;
        public override int RouteAnchorIndex => _routeAnchorIndex;
        public override float StateElapsed => _stateElapsed;

        public override void SetState(AmbientNpcRoutineState state) => _state = state;
        public override void SetPaused(bool isPaused) => _isPaused = isPaused;
        public override void SetRouteAnchorIndex(int index) => _routeAnchorIndex = index;
        public override void AdvanceElapsed(float deltaTime) => _stateElapsed += deltaTime > 0f ? deltaTime : 0f;
        public override void ResetElapsed() => _stateElapsed = 0f;

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

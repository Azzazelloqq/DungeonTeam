using System;

namespace Code.ApplicationRoot
{
    internal enum PlayerFlowState
    {
        Initializing,
        GuildHall,
        WorldMap,
        DungeonRun,
        Faulted,
        Disposed
    }

    internal sealed class ApplicationTransitionGate
    {
        private bool _isTransitioning;
        private bool _isDisposed;

        public ApplicationTransitionGate(PlayerFlowState initialState) => State = initialState;
        public PlayerFlowState State { get; private set; }

        public bool TryBegin(PlayerFlowState expectedState, out TransitionLease lease)
        {
            if (_isDisposed || _isTransitioning || State != expectedState)
            {
                lease = null;
                return false;
            }

            _isTransitioning = true;
            lease = new TransitionLease(this);
            return true;
        }

        public void Dispose()
        {
            _isDisposed = true;
            State = PlayerFlowState.Disposed;
            _isTransitioning = false;
        }

        internal void Complete(PlayerFlowState state)
        {
            if (_isDisposed)
            {
                return;
            }

            State = state;
            _isTransitioning = false;
        }

        internal sealed class TransitionLease : IDisposable
        {
            private ApplicationTransitionGate _owner;
            public TransitionLease(ApplicationTransitionGate owner) => _owner = owner;
            public void Complete(PlayerFlowState state)
            {
                var owner = _owner ?? throw new ObjectDisposedException(nameof(TransitionLease));
                _owner = null;
                owner.Complete(state);
            }
            public void Dispose()
            {
                var owner = _owner;
                _owner = null;
                owner?.Complete(owner.State);
            }
        }
    }
}

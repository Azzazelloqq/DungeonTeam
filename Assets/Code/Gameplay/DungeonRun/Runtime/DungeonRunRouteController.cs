using System;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunRouteController : IDisposable
    {
        private readonly DungeonRunRouteProgress _progress;
        private readonly Func<Vector3> _getLeaderPosition;
        private readonly ITickHandler _tickHandler;

        private bool _isInitialized;
        private bool _isDisposed;

        public DungeonRunRouteController(
            DungeonRunRouteProgress progress,
            Func<Vector3> getLeaderPosition,
            ITickHandler tickHandler)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _getLeaderPosition = getLeaderPosition ??
                throw new ArgumentNullException(nameof(getLeaderPosition));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
        }

        public event Action<DungeonRunRoutePhase> PhaseChanged;

        public DungeonRunRoutePhase Phase => _progress.Phase;

        public int NextCheckpointIndex => _progress.NextCheckpointIndex;

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DungeonRunRouteController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "Dungeon Run route controller is already initialized.");
            }

            _progress.PhaseChanged += OnPhaseChanged;
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
            AdvanceRoute();
        }

        public bool CompleteEncounter()
        {
            return !_isDisposed && _progress.CompleteEncounter();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _progress.PhaseChanged -= OnPhaseChanged;
            }

            PhaseChanged = null;
        }

        private void OnFrameUpdate(float deltaTime)
        {
            AdvanceRoute();
        }

        private void AdvanceRoute()
        {
            var position = _getLeaderPosition();
            var routePosition = new DungeonRunRoutePoint(position.x, position.z);
            while (_progress.NextCheckpointIndex < _progress.CheckpointCount &&
                   _progress.TryReachCheckpoint(
                       _progress.NextCheckpointIndex,
                       routePosition))
            {
            }
        }

        private void OnPhaseChanged(DungeonRunRoutePhase phase)
        {
            PhaseChanged?.Invoke(phase);
        }
    }
}

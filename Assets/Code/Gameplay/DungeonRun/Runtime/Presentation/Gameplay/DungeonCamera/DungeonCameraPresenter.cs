using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera
{
    internal sealed class DungeonCameraPresenter : DungeonCameraPresenterBase
    {
        private readonly Func<Vector3> _leaderPosition;
        private readonly ITickHandler _tickHandler;

        private bool _isSubscribed;

        public DungeonCameraPresenter(
            DungeonCameraViewBase view,
            DungeonCameraModelBase model,
            Func<Vector3> leaderPosition,
            ITickHandler tickHandler)
            : base(view, model)
        {
            _leaderPosition = leaderPosition ??
                throw new ArgumentNullException(nameof(leaderPosition));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
        }

        protected override void OnInitialize()
        {
            StartPresenting();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            StartPresenting();
            return default;
        }

        protected override void OnDispose()
        {
            StopPresenting();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            StopPresenting();
            return default;
        }

        private void StartPresenting()
        {
            if (_isSubscribed)
            {
                throw new InvalidOperationException(
                    "Dungeon Camera Presenter is already initialized.");
            }

            view.ApplyPose(model.Snap(_leaderPosition()));
            _tickHandler.SubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            _isSubscribed = true;
        }

        private void StopPresenting()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _tickHandler.UnsubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            _isSubscribed = false;
        }

        private void OnFrameLateUpdate(float deltaTime)
        {
            view.ApplyPose(model.Advance(_leaderPosition(), deltaTime));
        }
    }
}

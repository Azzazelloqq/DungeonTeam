using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera
{
    internal sealed class DungeonCameraView : DungeonCameraViewBase
    {
        private readonly Camera _camera;

        public DungeonCameraView(Camera camera)
        {
            _camera = camera != null
                ? camera
                : throw new ArgumentNullException(nameof(camera));
        }

        public override void ApplyPose(DungeonCameraPose pose)
        {
            _camera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}

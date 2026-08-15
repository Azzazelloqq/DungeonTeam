using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera.Base;
using NUnit.Framework;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Tests
{
    public sealed class DungeonCameraTests
    {
        [Test]
        public void Snap_WithoutAuthoredSpatialData_UsesLegacyFixedYaw()
        {
            var settings = Settings();
            var model = new DungeonCameraModel(settings, DungeonSpatialLayout.Empty);

            var pose = model.Snap(new Vector3(2f, 0f, 3f));

            var expectedRotation = Quaternion.Euler(55f, 45f, 0f);
            var expectedPosition = new Vector3(2f, 1f, 3f) +
                                   expectedRotation * Vector3.back * 10f;
            Assert.That(Vector3.Distance(pose.Position, expectedPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(pose.Rotation, expectedRotation), Is.LessThan(0.001f));
        }

        [Test]
        public void Snap_OnAuthoredStraight_UsesRouteTangentInsteadOfFallbackYaw()
        {
            var model = new DungeonCameraModel(Settings(), CreateTurnLayout());

            var pose = model.Snap(Vector3.zero);

            var planarForward = Vector3.ProjectOnPlane(
                pose.Rotation * Vector3.forward,
                Vector3.up).normalized;
            Assert.That(Vector3.Dot(planarForward, Vector3.forward), Is.GreaterThan(0.999f));
        }

        [Test]
        public void Snap_AtTurnShot_BlendsRouteTangentAndUsesAuthoredAnchor()
        {
            var model = new DungeonCameraModel(Settings(), CreateTurnLayout());

            var pose = model.Snap(new Vector3(0f, 0f, 10f));

            Assert.That(
                Vector3.Distance(pose.Position, new Vector3(-5f, 4f, 10f)),
                Is.LessThan(0.001f));
            var planarForward = Vector3.ProjectOnPlane(
                pose.Rotation * Vector3.forward,
                Vector3.up).normalized;
            Assert.That(planarForward.x, Is.GreaterThan(0.2f));
            Assert.That(planarForward.z, Is.GreaterThan(0.2f));
        }

        [Test]
        public void Snap_AtOverlappingShots_UsesBothAuthoredAnchors()
        {
            var model = new DungeonCameraModel(Settings(), CreateOverlappingShotLayout());

            var pose = model.Snap(new Vector3(5f, 0f, 10f));

            Assert.That(
                Vector3.Distance(pose.Position, new Vector3(5f, 4f, 10f)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void Presenter_AfterDispose_StopsApplyingLateUpdateFrames()
        {
            var dispatcher = new ManualDispatcher();
            var tickHandler = new UnityTickHandler(dispatcher);
            var view = new FakeCameraView();
            var model = new DungeonCameraModel(Settings(), DungeonSpatialLayout.Empty);
            var leaderPosition = Vector3.zero;
            var presenter = new DungeonCameraPresenter(
                view,
                model,
                () => leaderPosition,
                tickHandler);

            presenter.Initialize();
            leaderPosition = Vector3.right;
            dispatcher.RaiseLateUpdate(0.1f);
            Assert.That(view.AppliedPoseCount, Is.EqualTo(2));

            presenter.Dispose();
            dispatcher.RaiseLateUpdate(0.1f);

            Assert.That(view.AppliedPoseCount, Is.EqualTo(2));
            tickHandler.Dispose();
            dispatcher.Dispose();
        }

        private static DungeonRunCameraSettings Settings()
        {
            return new DungeonRunCameraSettings(
                distance: 10f,
                pitch: 55f,
                fallbackYaw: 45f,
                targetHeight: 1f,
                followSharpness: 10f);
        }

        private static DungeonSpatialLayout CreateTurnLayout()
        {
            return new DungeonSpatialLayout(
                new[]
                {
                    Pose(0f, 0f, 0f),
                    Pose(0f, 0f, 10f),
                    Pose(10f, 0f, 10f)
                },
                new[]
                {
                    new DungeonCameraShot(
                        Pose(-5f, 4f, 10f),
                        routeCheckpointIndex: 1,
                        lookAheadDistance: 3f,
                        activationRange: 4f,
                        blendRange: 4f)
                },
                new DungeonEncounterSpan(
                    Pose(0f, 0f, 10f),
                    Pose(10f, 0f, 10f),
                    startCheckpointIndex: 1,
                    endCheckpointIndex: 2),
                new[] { new DungeonVector3(1f, 0f, -1f) },
                new[] { Pose(5f, 0f, 10f) });
        }

        private static DungeonSpatialLayout CreateOverlappingShotLayout()
        {
            return new DungeonSpatialLayout(
                new[]
                {
                    Pose(0f, 0f, 0f),
                    Pose(0f, 0f, 10f),
                    Pose(10f, 0f, 10f),
                    Pose(10f, 0f, 20f)
                },
                new[]
                {
                    new DungeonCameraShot(
                        Pose(-5f, 4f, 10f),
                        routeCheckpointIndex: 1,
                        lookAheadDistance: 3f,
                        activationRange: 8f,
                        blendRange: 3f),
                    new DungeonCameraShot(
                        Pose(15f, 4f, 10f),
                        routeCheckpointIndex: 2,
                        lookAheadDistance: 3f,
                        activationRange: 8f,
                        blendRange: 3f)
                },
                new DungeonEncounterSpan(
                    Pose(0f, 0f, 10f),
                    Pose(10f, 0f, 20f),
                    startCheckpointIndex: 1,
                    endCheckpointIndex: 3),
                new[] { new DungeonVector3(1f, 0f, -1f) },
                new[] { Pose(5f, 0f, 10f) });
        }

        private static DungeonPose Pose(float x, float y, float z)
        {
            return new DungeonPose(x, y, z, 0f, 0f, 0f, 1f);
        }

        private sealed class FakeCameraView : DungeonCameraViewBase
        {
            public int AppliedPoseCount { get; private set; }

            public override void ApplyPose(DungeonCameraPose pose)
            {
                AppliedPoseCount++;
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

        private sealed class ManualDispatcher : IDispatcher
        {
            public event Action<float> OnUpdate;
            public event Action<float> OnLateUpdate;
            public event Action<float> OnFixedUpdate;
            public event Action<float> OnEndFrameUpdate;

            public float DeltaTime { get; private set; }

            public void RaiseLateUpdate(float deltaTime)
            {
                DeltaTime = deltaTime;
                OnLateUpdate?.Invoke(deltaTime);
            }

            public void Dispose()
            {
                OnUpdate = null;
                OnLateUpdate = null;
                OnFixedUpdate = null;
                OnEndFrameUpdate = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class WallOcclusionController : IDisposable
    {
        internal const int MaxTargetCount = 16;

        private static readonly int TargetCountId = Shader.PropertyToID(
            "_WallOcclusionTargetCount");
        private static readonly int TargetsId = Shader.PropertyToID(
            "_WallOcclusionTargets");
        private static readonly int RadiusId = Shader.PropertyToID(
            "_WallOcclusionRadius");
        private static readonly int FeatherId = Shader.PropertyToID(
            "_WallOcclusionFeather");
        private static readonly int DepthBiasId = Shader.PropertyToID(
            "_WallOcclusionDepthBias");

        private readonly Camera _camera;
        private readonly IReadOnlyList<ActorInstance> _targets;
        private readonly ITickHandler _tickHandler;
        private readonly DungeonRunBindings _bindings;
        private readonly Vector4[] _projectedTargets = new Vector4[MaxTargetCount];

        private float _appliedRadius = float.NaN;
        private float _appliedFeather = float.NaN;
        private float _appliedDepthBias = float.NaN;
        private bool _isInitialized;
        private bool _isDisposed;

        public WallOcclusionController(
            Camera camera,
            IReadOnlyList<ActorInstance> targets,
            ITickHandler tickHandler,
            DungeonRunBindings bindings)
        {
            _camera = camera != null
                ? camera
                : throw new ArgumentNullException(nameof(camera));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(WallOcclusionController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "Wall Occlusion Controller is already initialized.");
            }

            UpdateShaderSettings();
            UpdateShaderTargets();
            _tickHandler.SubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            _isInitialized = true;
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
                _tickHandler.UnsubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            }

            Shader.SetGlobalInt(TargetCountId, 0);
        }

        private void OnFrameLateUpdate(float deltaTime)
        {
            UpdateShaderSettings();
            UpdateShaderTargets();
        }

        private void UpdateShaderSettings()
        {
            _bindings.ValidateWallOcclusionSettings();

            var radius = _bindings.WallOcclusionRadius;
            if (_appliedRadius != radius)
            {
                Shader.SetGlobalFloat(RadiusId, radius);
                _appliedRadius = radius;
            }

            var feather = _bindings.WallOcclusionFeather;
            if (_appliedFeather != feather)
            {
                Shader.SetGlobalFloat(FeatherId, feather);
                _appliedFeather = feather;
            }

            var depthBias = _bindings.WallOcclusionDepthBias;
            if (_appliedDepthBias != depthBias)
            {
                Shader.SetGlobalFloat(DepthBiasId, depthBias);
                _appliedDepthBias = depthBias;
            }
        }

        private void UpdateShaderTargets()
        {
            if (_targets.Count > MaxTargetCount)
            {
                throw new InvalidOperationException(
                    $"Wall occlusion supports up to {MaxTargetCount} heroes, " +
                    $"but {_targets.Count} were provided.");
            }

            var projectedTargetCount = 0;
            for (var index = 0; index < _targets.Count; index++)
            {
                var target = _targets[index];
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var worldPosition = target.Position +
                    Vector3.up * _bindings.WallOcclusionTargetHeight;
                var viewportPosition = _camera.WorldToViewportPoint(worldPosition);
                if (viewportPosition.z <= 0f)
                {
                    continue;
                }

                _projectedTargets[projectedTargetCount++] = new Vector4(
                    viewportPosition.x,
                    viewportPosition.y,
                    viewportPosition.z,
                    0f);
            }

            Shader.SetGlobalVectorArray(TargetsId, _projectedTargets);
            Shader.SetGlobalInt(TargetCountId, projectedTargetCount);
        }
    }
}

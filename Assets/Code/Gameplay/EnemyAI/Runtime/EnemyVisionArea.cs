using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonTeam.Gameplay.EnemyAI.Runtime
{
    internal sealed class EnemyVisionArea : IDisposable
    {
        private const int SegmentCount = 24;
        private const float ObstacleRefreshInterval = 0.1f;
        private const string ShaderName = "Universal Render Pipeline/Unlit";

        private readonly Color _idleColor;
        private readonly Color _alertColor;
        private readonly Vector3[] _directions;
        private readonly Vector3[] _vertices;
        private readonly float _viewDistance;
        private readonly float _eyeHeight;
        private readonly int _obstacleMask;
        private readonly GameObject _gameObject;
        private readonly Mesh _mesh;
        private readonly Material _material;

        private bool _isAlerted;
        private bool _isDisposed;
        private float _obstacleRefreshCooldown;

        public EnemyVisionArea(EnemyAiSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Enemy vision area requires shader '{ShaderName}'.");
            }

            _idleColor = settings.IdleVisionColor;
            _alertColor = settings.AlertVisionColor;
            _viewDistance = settings.ViewDistance;
            _eyeHeight = settings.EyeHeight;
            _obstacleMask = settings.ObstacleMask;
            _directions = CreateDirections(settings.ViewAngle);
            _vertices = new Vector3[SegmentCount + 2];
            _mesh = CreateMesh(_directions, _vertices, _viewDistance);
            _material = CreateMaterial(shader, _idleColor);
            _gameObject = CreateGameObject(_mesh, _material);
        }

        public void SetVisible(bool visible)
        {
            _gameObject.SetActive(visible);
        }

        public void UpdatePose(
            Vector3 position,
            Vector3 forward,
            float height,
            float deltaTime)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            _gameObject.transform.SetPositionAndRotation(
                position + Vector3.up * height,
                rotation);

            _obstacleRefreshCooldown -= deltaTime;
            if (_obstacleRefreshCooldown > 0f)
            {
                return;
            }

            _obstacleRefreshCooldown = ObstacleRefreshInterval;

            var rayOrigin = position + Vector3.up * _eyeHeight;
            for (var index = 0; index < _directions.Length; index++)
            {
                var worldDirection = rotation * _directions[index];
                var distance = Physics.Raycast(
                    rayOrigin,
                    worldDirection,
                    out var hit,
                    _viewDistance,
                    _obstacleMask,
                    QueryTriggerInteraction.Ignore)
                    ? hit.distance
                    : _viewDistance;
                _vertices[index + 1] = _directions[index] * distance;
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        public void SetAlerted(bool alerted)
        {
            if (_isAlerted == alerted)
            {
                return;
            }

            _isAlerted = alerted;
            _material.SetColor("_BaseColor", alerted ? _alertColor : _idleColor);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Destroy(_gameObject);
            Destroy(_material);
            Destroy(_mesh);
        }

        private static Vector3[] CreateDirections(float angle)
        {
            var directions = new Vector3[SegmentCount + 1];
            var halfAngle = angle * 0.5f;
            for (var index = 0; index <= SegmentCount; index++)
            {
                var normalized = index / (float)SegmentCount;
                var radians = Mathf.Lerp(-halfAngle, halfAngle, normalized) * Mathf.Deg2Rad;
                directions[index] = new Vector3(
                    Mathf.Sin(radians),
                    0f,
                    Mathf.Cos(radians));
            }

            return directions;
        }

        private static Mesh CreateMesh(
            Vector3[] directions,
            Vector3[] vertices,
            float distance)
        {
            var triangles = new int[SegmentCount * 3];
            vertices[0] = Vector3.zero;
            for (var index = 0; index <= SegmentCount; index++)
            {
                vertices[index + 1] = directions[index] * distance;

                if (index == SegmentCount)
                {
                    continue;
                }

                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = index + 2;
            }

            var mesh = new Mesh
            {
                name = "EnemyVisionAreaMesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.MarkDynamic();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader)
            {
                name = "EnemyVisionAreaMaterial",
                renderQueue = (int)RenderQueue.Transparent
            };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static GameObject CreateGameObject(Mesh mesh, Material material)
        {
            var gameObject = new GameObject("EnemyVisionArea")
            {
                layer = LayerMask.NameToLayer("Ignore Raycast")
            };
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return gameObject;
        }

        private static void Destroy(UnityEngine.Object instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}

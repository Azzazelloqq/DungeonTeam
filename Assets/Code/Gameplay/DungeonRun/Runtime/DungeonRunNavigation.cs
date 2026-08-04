using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunNavigation : IDisposable
    {
        private const float SpawnSampleDistance = 3f;

        private GameObject _root;
        private NavMeshSurface _surface;

        public void Build()
        {
            if (_root != null)
            {
                throw new InvalidOperationException("Dungeon Run navigation is already built.");
            }

            _root = new GameObject("DungeonRunNavigation");
            _surface = _root.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.All;
            _surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            _surface.BuildNavMesh();

            if (_surface.navMeshData == null)
            {
                Dispose();
                throw new InvalidOperationException(
                    "Dungeon Run could not build navigation from the assembled map geometry.");
            }
        }

        public Vector3 RequireSpawnPosition(Vector3 requestedPosition)
        {
            if (_surface == null || _surface.navMeshData == null)
            {
                throw new InvalidOperationException("Dungeon Run navigation is not built.");
            }

            if (!NavMesh.SamplePosition(
                    requestedPosition,
                    out var hit,
                    SpawnSampleDistance,
                    NavMesh.AllAreas))
            {
                throw new InvalidOperationException(
                    $"Dungeon Run spawn position {requestedPosition} is outside the NavMesh.");
            }

            return hit.position;
        }

        public void Dispose()
        {
            var surface = _surface;
            var root = _root;
            _surface = null;
            _root = null;

            surface?.RemoveData();
            if (root == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}

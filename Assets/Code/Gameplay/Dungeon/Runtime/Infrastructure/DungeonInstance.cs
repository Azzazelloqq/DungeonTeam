using System;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal sealed class DungeonInstance : IDungeonInstance
    {
        private GameObject _mapRoot;
        private GameObject[] _addressableInstances;
        private AsyncOperationHandle[] _assetHandles;
        private readonly bool _destroyMapRoot;
        private bool _disposed;

        public DungeonInstance(
            GameObject mapRoot,
            DungeonMapSnapshot mapSnapshot,
            DungeonContentPlan contentPlan)
        {
            _mapRoot = mapRoot;
            _addressableInstances = new[] { mapRoot };
            _assetHandles = Array.Empty<AsyncOperationHandle>();
            _destroyMapRoot = false;
            MapSnapshot = mapSnapshot;
            ContentPlan = contentPlan;
        }

        public DungeonInstance(
            GameObject mapRoot,
            GameObject[] chunkInstances,
            DungeonMapSnapshot mapSnapshot,
            DungeonContentPlan contentPlan)
        {
            _mapRoot = mapRoot;
            _addressableInstances = chunkInstances;
            _assetHandles = Array.Empty<AsyncOperationHandle>();
            _destroyMapRoot = true;
            MapSnapshot = mapSnapshot;
            ContentPlan = contentPlan;
        }

        public DungeonInstance(
            GameObject mapRoot,
            AsyncOperationHandle tileSetHandle,
            DungeonMapSnapshot mapSnapshot,
            DungeonContentPlan contentPlan)
        {
            _mapRoot = mapRoot;
            _addressableInstances = Array.Empty<GameObject>();
            _assetHandles = new[] { tileSetHandle };
            _destroyMapRoot = true;
            MapSnapshot = mapSnapshot;
            ContentPlan = contentPlan;
        }

        public DungeonMapSnapshot MapSnapshot { get; }
        public DungeonContentPlan ContentPlan { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (var index = _addressableInstances.Length - 1; index >= 0; index--)
            {
                var instance = _addressableInstances[index];
                if (instance != null)
                {
                    Addressables.ReleaseInstance(instance);
                }
            }

            if (_destroyMapRoot && _mapRoot != null)
            {
                UnityEngine.Object.Destroy(_mapRoot);
            }

            for (var index = _assetHandles.Length - 1; index >= 0; index--)
            {
                if (_assetHandles[index].IsValid())
                {
                    Addressables.Release(_assetHandles[index]);
                }
            }

            _addressableInstances = null;
            _assetHandles = null;
            _mapRoot = null;
        }
    }
}

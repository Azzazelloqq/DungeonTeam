using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal sealed class DungeonInstance : IDungeonInstance
    {
        private GameObject _mapRoot;
        private GameObject[] _addressableInstances;
        private readonly bool _destroyMapRoot;

        public DungeonInstance(
            GameObject mapRoot,
            DungeonMapSnapshot mapSnapshot,
            DungeonContentPlan contentPlan)
        {
            _mapRoot = mapRoot;
            _addressableInstances = new[] { mapRoot };
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
            _destroyMapRoot = true;
            MapSnapshot = mapSnapshot;
            ContentPlan = contentPlan;
        }

        public DungeonMapSnapshot MapSnapshot { get; }
        public DungeonContentPlan ContentPlan { get; }

        public void Dispose()
        {
            if (_addressableInstances == null)
            {
                return;
            }

            for (var index = _addressableInstances.Length - 1; index >= 0; index--)
            {
                var instance = _addressableInstances[index];
                if (instance != null)
                {
                    Addressables.ReleaseInstance(instance);
                }
            }

            _addressableInstances = null;
            if (_destroyMapRoot && _mapRoot != null)
            {
                Object.Destroy(_mapRoot);
            }

            _mapRoot = null;
        }
    }
}

using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal sealed class DungeonInstance : IDungeonInstance
    {
        private GameObject _mapRoot;

        public DungeonInstance(
            GameObject mapRoot,
            DungeonMapSnapshot mapSnapshot,
            DungeonContentPlan contentPlan)
        {
            _mapRoot = mapRoot;
            MapSnapshot = mapSnapshot;
            ContentPlan = contentPlan;
        }

        public DungeonMapSnapshot MapSnapshot { get; }
        public DungeonContentPlan ContentPlan { get; }

        public void Dispose()
        {
            if (_mapRoot == null)
            {
                return;
            }

            Addressables.ReleaseInstance(_mapRoot);
            _mapRoot = null;
        }
    }
}

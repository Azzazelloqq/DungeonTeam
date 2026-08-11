using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public sealed class ChestViewLoader : IChestViewLoader
    {
        private readonly IResourceLoader _resourceLoader;

        public ChestViewLoader([Inject] IResourceLoader resourceLoader)
        {
            _resourceLoader = resourceLoader ??
                throw new ArgumentNullException(nameof(resourceLoader));
        }

        public bool Supports(string chestId)
        {
            return ChestViewAssetCatalog.TryResolveAddress(chestId, out _);
        }

        public async UniTask<ChestViewSet> LoadAsync(
            IReadOnlyList<string> chestIds,
            CancellationToken token)
        {
            if (chestIds == null)
            {
                throw new ArgumentNullException(nameof(chestIds));
            }

            var distinctIds = GetDistinctIds(chestIds);
            var views = new ChestViewBase[distinctIds.Count];
            var loadedPrefabs = new GameObject[distinctIds.Count];
            var loadedCount = 0;
            try
            {
                for (var index = 0; index < distinctIds.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var chestId = distinctIds[index];
                    var prefabObject = await _resourceLoader.LoadResourceAsync<GameObject>(
                        ChestViewAssetCatalog.ResolveAddress(chestId),
                        token);
                    if (prefabObject != null)
                    {
                        loadedPrefabs[loadedCount++] = prefabObject;
                    }

                    token.ThrowIfCancellationRequested();
                    if (prefabObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Chest prefab for ID '{chestId}' could not be loaded.");
                    }

                    var view = prefabObject.GetComponent<ChestViewBase>();
                    if (view == null)
                    {
                        throw new InvalidOperationException(
                            $"Chest prefab for ID '{chestId}' requires a " +
                            $"{nameof(ChestViewBase)} component on its root.");
                    }

                    views[index] = view;
                }

                return new ChestViewSet(
                    distinctIds,
                    views,
                    _resourceLoader,
                    loadedPrefabs);
            }
            catch
            {
                for (var index = loadedCount - 1; index >= 0; index--)
                {
                    _resourceLoader.ReleaseResource(loadedPrefabs[index]);
                }

                throw;
            }
        }

        private static List<string> GetDistinctIds(IReadOnlyList<string> chestIds)
        {
            var distinctIds = new List<string>(chestIds.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < chestIds.Count; index++)
            {
                var chestId = chestIds[index];
                if (string.IsNullOrWhiteSpace(chestId))
                {
                    throw new ArgumentException(
                        $"Chest ID at index {index} cannot be empty.",
                        nameof(chestIds));
                }

                if (seenIds.Add(chestId))
                {
                    distinctIds.Add(chestId);
                }
            }

            return distinctIds;
        }
    }
}

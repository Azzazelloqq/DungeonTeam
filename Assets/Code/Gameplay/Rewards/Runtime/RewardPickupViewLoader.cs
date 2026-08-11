using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardPickupViewLoader : IRewardPickupViewLoader
    {
        private readonly IResourceLoader _resourceLoader;

        public RewardPickupViewLoader([Inject] IResourceLoader resourceLoader)
        {
            _resourceLoader = resourceLoader ??
                throw new ArgumentNullException(nameof(resourceLoader));
        }

        public async UniTask<RewardPickupViewSet> LoadAsync(
            IReadOnlyList<string> rewardIds,
            CancellationToken token)
        {
            if (rewardIds == null)
            {
                throw new ArgumentNullException(nameof(rewardIds));
            }

            var distinctIds = GetDistinctIds(rewardIds);
            var views = new RewardPickupViewBase[distinctIds.Count];
            var loadedPrefabs = new GameObject[distinctIds.Count];
            var loadedCount = 0;
            try
            {
                for (var index = 0; index < distinctIds.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var rewardId = distinctIds[index];
                    var prefabObject = await _resourceLoader.LoadResourceAsync<GameObject>(
                        RewardPickupViewAssetCatalog.ResolveAddress(rewardId),
                        token);
                    if (prefabObject != null)
                    {
                        loadedPrefabs[loadedCount++] = prefabObject;
                    }

                    token.ThrowIfCancellationRequested();
                    if (prefabObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Reward Pickup prefab for ID '{rewardId}' could not be loaded.");
                    }

                    var view = prefabObject.GetComponent<RewardPickupViewBase>();
                    if (view == null)
                    {
                        throw new InvalidOperationException(
                            $"Reward Pickup prefab for ID '{rewardId}' requires a " +
                            $"{nameof(RewardPickupViewBase)} component on its root.");
                    }

                    views[index] = view;
                }

                return new RewardPickupViewSet(
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

        private static List<string> GetDistinctIds(IReadOnlyList<string> rewardIds)
        {
            var distinctIds = new List<string>(rewardIds.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < rewardIds.Count; index++)
            {
                var rewardId = rewardIds[index];
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    throw new ArgumentException(
                        $"Reward ID at index {index} cannot be empty.",
                        nameof(rewardIds));
                }

                if (seenIds.Add(rewardId))
                {
                    distinctIds.Add(rewardId);
                }
            }

            return distinctIds;
        }
    }
}

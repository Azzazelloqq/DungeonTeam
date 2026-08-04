using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardPickupViewSet : IDisposable
    {
        private Dictionary<string, RewardPickupViewBase> _views;
        private IResourceLoader _resourceLoader;
        private GameObject[] _loadedPrefabs;

        public RewardPickupViewSet(
            IReadOnlyList<string> rewardIds,
            RewardPickupViewBase[] views)
            : this(rewardIds, views, resourceLoader: null, loadedPrefabs: null)
        {
        }

        internal RewardPickupViewSet(
            IReadOnlyList<string> rewardIds,
            RewardPickupViewBase[] views,
            IResourceLoader resourceLoader,
            GameObject[] loadedPrefabs)
        {
            if (rewardIds == null)
            {
                throw new ArgumentNullException(nameof(rewardIds));
            }

            if (views == null)
            {
                throw new ArgumentNullException(nameof(views));
            }

            if (rewardIds.Count != views.Length)
            {
                throw new ArgumentException(
                    "Reward IDs and loaded views must have the same count.");
            }

            _views = new Dictionary<string, RewardPickupViewBase>(
                views.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < views.Length; index++)
            {
                var view = views[index] != null
                    ? views[index]
                    : throw new ArgumentException(
                        $"Reward Pickup view at index {index} is missing.",
                        nameof(views));
                if (!_views.TryAdd(rewardIds[index], view))
                {
                    throw new ArgumentException(
                        $"Reward ID '{rewardIds[index]}' was loaded more than once.");
                }
            }

            if ((resourceLoader == null) != (loadedPrefabs == null))
            {
                throw new ArgumentException(
                    "Resource loader and loaded prefabs must be provided together.");
            }

            _resourceLoader = resourceLoader;
            _loadedPrefabs = loadedPrefabs;
        }

        public bool IsDisposed => _views == null;

        public RewardPickupViewBase Require(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID cannot be empty.", nameof(rewardId));
            }

            var views = _views ?? throw new ObjectDisposedException(nameof(RewardPickupViewSet));
            if (!views.TryGetValue(rewardId, out var view))
            {
                throw new InvalidOperationException(
                    $"Loaded Reward Pickup views do not contain reward ID '{rewardId}'.");
            }

            return view;
        }

        public void Dispose()
        {
            var loadedPrefabs = _loadedPrefabs;
            var resourceLoader = _resourceLoader;
            _views = null;
            _loadedPrefabs = null;
            _resourceLoader = null;

            if (loadedPrefabs == null)
            {
                return;
            }

            for (var index = loadedPrefabs.Length - 1; index >= 0; index--)
            {
                resourceLoader.ReleaseResource(loadedPrefabs[index]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public sealed class ChestViewSet : IDisposable
    {
        private Dictionary<string, ChestViewBase> _views;
        private IResourceLoader _resourceLoader;
        private GameObject[] _loadedPrefabs;

        public ChestViewSet(IReadOnlyList<string> chestIds, ChestViewBase[] views)
            : this(chestIds, views, resourceLoader: null, loadedPrefabs: null)
        {
        }

        internal ChestViewSet(
            IReadOnlyList<string> chestIds,
            ChestViewBase[] views,
            IResourceLoader resourceLoader,
            GameObject[] loadedPrefabs)
        {
            if (chestIds == null)
            {
                throw new ArgumentNullException(nameof(chestIds));
            }

            if (views == null)
            {
                throw new ArgumentNullException(nameof(views));
            }

            if (chestIds.Count != views.Length)
            {
                throw new ArgumentException(
                    "Chest IDs and loaded views must have the same count.");
            }

            _views = new Dictionary<string, ChestViewBase>(views.Length, StringComparer.Ordinal);
            for (var index = 0; index < views.Length; index++)
            {
                var view = views[index] != null
                    ? views[index]
                    : throw new ArgumentException(
                        $"Chest view at index {index} is missing.",
                        nameof(views));
                if (!_views.TryAdd(chestIds[index], view))
                {
                    throw new ArgumentException(
                        $"Chest ID '{chestIds[index]}' was loaded more than once.");
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

        public ChestViewBase Require(string chestId)
        {
            if (string.IsNullOrWhiteSpace(chestId))
            {
                throw new ArgumentException("Chest ID cannot be empty.", nameof(chestId));
            }

            var views = _views ?? throw new ObjectDisposedException(nameof(ChestViewSet));
            if (!views.TryGetValue(chestId, out var view))
            {
                throw new InvalidOperationException(
                    $"Loaded chest views do not contain chest ID '{chestId}'.");
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

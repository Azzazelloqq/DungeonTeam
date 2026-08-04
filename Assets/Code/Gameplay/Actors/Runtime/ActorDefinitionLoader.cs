using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorDefinitionLoader : IActorDefinitionLoader
    {
        private readonly ActorConfigCatalog _catalog;
        private readonly IResourceLoader _resourceLoader;

        public ActorDefinitionLoader(
            ActorConfigCatalog catalog,
            IResourceLoader resourceLoader)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resourceLoader = resourceLoader ??
                throw new ArgumentNullException(nameof(resourceLoader));
        }

        public async UniTask<ActorDefinitionSet> LoadAsync(
            IReadOnlyList<string> actorIds,
            CancellationToken token)
        {
            if (actorIds == null)
            {
                throw new ArgumentNullException(nameof(actorIds));
            }

            var distinctIds = GetDistinctIds(actorIds);
            var definitions = new ActorDefinition[distinctIds.Count];
            var loadedPrefabs = new GameObject[distinctIds.Count];
            var loadedCount = 0;
            try
            {
                for (var index = 0; index < distinctIds.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var config = _catalog.Require(distinctIds[index]);
                    var prefabObject = await _resourceLoader.LoadResourceAsync<GameObject>(
                        ActorViewAssetCatalog.ResolveAddress(config.ActorId),
                        token);
                    if (prefabObject != null)
                    {
                        loadedPrefabs[loadedCount++] = prefabObject;
                    }

                    token.ThrowIfCancellationRequested();
                    if (prefabObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Actor prefab for ID '{config.ActorId}' could not be loaded.");
                    }

                    var prefab = prefabObject.GetComponent<ActorViewBase>();
                    if (prefab == null)
                    {
                        throw new InvalidOperationException(
                            $"Actor prefab for ID '{config.ActorId}' requires an " +
                            $"{nameof(ActorViewBase)} component on its root.");
                    }

                    definitions[index] = new ActorDefinition(
                        config.ActorId,
                        prefab,
                        config.MaximumHealth,
                        config.MovementSpeed);
                }

                return new ActorDefinitionSet(
                    definitions,
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

        private static List<string> GetDistinctIds(IReadOnlyList<string> actorIds)
        {
            var distinctIds = new List<string>(actorIds.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < actorIds.Count; index++)
            {
                var actorId = actorIds[index];
                if (string.IsNullOrWhiteSpace(actorId))
                {
                    throw new ArgumentException(
                        $"Actor ID at index {index} cannot be empty.",
                        nameof(actorIds));
                }

                if (seenIds.Add(actorId))
                {
                    distinctIds.Add(actorId);
                }
            }

            return distinctIds;
        }
    }
}

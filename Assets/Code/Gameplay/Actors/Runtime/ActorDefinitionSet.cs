using System;
using System.Collections.Generic;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorDefinitionSet : IDisposable
    {
        private Dictionary<string, ActorDefinition> _definitions;
        private IResourceLoader _resourceLoader;
        private GameObject[] _loadedPrefabs;

        public ActorDefinitionSet(ActorDefinition[] definitions)
            : this(definitions, resourceLoader: null, loadedPrefabs: null)
        {
        }

        internal ActorDefinitionSet(
            ActorDefinition[] definitions,
            IResourceLoader resourceLoader,
            GameObject[] loadedPrefabs)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new Dictionary<string, ActorDefinition>(
                definitions.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Loaded actor definition at index {index} is missing.",
                    nameof(definitions));
                if (!_definitions.TryAdd(definition.ActorId, definition))
                {
                    throw new ArgumentException(
                        $"Actor ID '{definition.ActorId}' was loaded more than once.",
                        nameof(definitions));
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

        public bool IsDisposed => _definitions == null;

        public ActorDefinition Require(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            var definitions = _definitions ?? throw new ObjectDisposedException(
                nameof(ActorDefinitionSet));
            if (!definitions.TryGetValue(actorId, out var definition))
            {
                throw new InvalidOperationException(
                    $"Loaded actor definitions do not contain actor ID '{actorId}'.");
            }

            return definition;
        }

        public void Dispose()
        {
            var loadedPrefabs = _loadedPrefabs;
            var resourceLoader = _resourceLoader;
            _definitions = null;
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

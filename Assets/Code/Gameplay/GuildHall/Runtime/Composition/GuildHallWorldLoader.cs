using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Addressables.Generated;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Composition
{
    public sealed class GuildHallWorldLoader
    {
        private readonly IResourceLoader _resourceLoader;

        public GuildHallWorldLoader(IResourceLoader resourceLoader)
        {
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        }

        public async Task<GuildHallWorldLease> LoadAsync(CancellationToken token)
        {
            GameObject prefab = null;
            GameObject instance = null;
            try
            {
                prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
                    AddressableIds.GuildHall.GuildHallGraybox,
                    token);
                if (prefab == null)
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidOperationException("Guild Hall Addressable prefab failed to load.");
                }

                if (prefab.activeSelf)
                {
                    throw new InvalidOperationException(
                        "Guild Hall prefab root must be inactive before instantiation.");
                }

                var rootViews = prefab.GetComponents<GuildHallViewBase>();
                if (rootViews.Length != 1)
                {
                    throw new InvalidOperationException(
                        "Guild Hall prefab requires exactly one GuildHallViewBase on its root.");
                }

                token.ThrowIfCancellationRequested();
                instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = "GuildHall";
                var view = instance.GetComponent<GuildHallViewBase>();
                token.ThrowIfCancellationRequested();
                return new GuildHallWorldLease(_resourceLoader, prefab, instance, view);
            }
            catch
            {
                if (instance != null)
                {
                    Destroy(instance);
                }

                if (prefab != null)
                {
                    _resourceLoader.ReleaseResource(prefab);
                }

                throw;
            }
        }

        private static void Destroy(GameObject target)
        {
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    public sealed class GuildHallWorldLease : IDisposable
    {
        private IResourceLoader _resourceLoader;
        private GameObject _prefab;
        private GameObject _instance;

        internal GuildHallWorldLease(
            IResourceLoader resourceLoader,
            GameObject prefab,
            GameObject instance,
            GuildHallViewBase view)
        {
            _resourceLoader = resourceLoader;
            _prefab = prefab;
            _instance = instance;
            View = view ?? throw new ArgumentNullException(nameof(view));
        }

        public GuildHallViewBase View { get; }

        public void Activate()
        {
            if (_instance == null)
            {
                throw new ObjectDisposedException(nameof(GuildHallWorldLease));
            }

            _instance.SetActive(true);
        }

        public void Dispose()
        {
            var instance = _instance;
            var prefab = _prefab;
            var resourceLoader = _resourceLoader;
            _instance = null;
            _prefab = null;
            _resourceLoader = null;

            if (instance != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(instance);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            if (prefab != null)
            {
                resourceLoader.ReleaseResource(prefab);
            }
        }
    }
}

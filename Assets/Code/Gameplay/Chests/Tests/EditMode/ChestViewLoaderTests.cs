using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Chests.Runtime;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest;
using NUnit.Framework;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Tests
{
    public sealed class ChestViewLoaderTests
    {
        [Test]
        public async Task LoadAsync_DuplicateIds_LoadsOneViewAndReleasesOnce()
        {
            var prefab = CreatePrefab();
            var resourceLoader = new FakeResourceLoader(prefab);
            var loader = new ChestViewLoader(resourceLoader);
            ChestViewSet views = null;
            try
            {
                Assert.That(loader.Supports("interest.chest.basic"), Is.True);
                Assert.That(loader.Supports("interest.unknown"), Is.False);

                views = await loader.LoadAsync(
                    new[] { "interest.chest.basic", "interest.chest.basic" },
                    CancellationToken.None);

                Assert.That(resourceLoader.LoadCount, Is.EqualTo(1));
                Assert.That(views.Require("interest.chest.basic"), Is.SameAs(prefab.GetComponent<ChestView>()));

                views.Dispose();
                views.Dispose();

                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                views?.Dispose();
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void LoadAsync_CancelledAfterResourceReturns_ReleasesLoadedResource()
        {
            var prefab = CreatePrefab();
            using var cancellation = new CancellationTokenSource();
            var resourceLoader = new FakeResourceLoader(prefab, cancellation);
            var loader = new ChestViewLoader(resourceLoader);
            try
            {
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await loader.LoadAsync(
                        new[] { "interest.chest.basic" },
                        cancellation.Token));

                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        private static GameObject CreatePrefab()
        {
            var prefab = new GameObject("ChestViewLoaderTestPrefab");
            prefab.AddComponent<ChestView>();
            return prefab;
        }

        private sealed class FakeResourceLoader : IResourceLoader
        {
            private readonly GameObject _prefab;
            private readonly CancellationTokenSource _cancellationOnLoad;

            public FakeResourceLoader(
                GameObject prefab,
                CancellationTokenSource cancellationOnLoad = null)
            {
                _prefab = prefab;
                _cancellationOnLoad = cancellationOnLoad;
            }

            public int LoadCount { get; private set; }

            public int ReleaseCount { get; private set; }

            public Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token) =>
                throw new NotSupportedException();

            public TResource LoadResource<TResource>(string resourceId) =>
                throw new NotSupportedException();

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token) => throw new NotSupportedException();

            public Task<TResource> LoadResourceAsync<TResource>(
                string resourceId,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                LoadCount++;
                _cancellationOnLoad?.Cancel();
                return Task.FromResult((TResource)(object)_prefab);
            }

            public Task<TComponent> LoadAndCreateAsync<TComponent, TParent>(
                string resourceId,
                TParent parent,
                CancellationToken token = default) => throw new NotSupportedException();

            public void ReleaseResource<TResource>(TResource resource)
            {
                Assert.That(resource, Is.SameAs(_prefab));
                ReleaseCount++;
            }

            public void ReleaseAllResources()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}

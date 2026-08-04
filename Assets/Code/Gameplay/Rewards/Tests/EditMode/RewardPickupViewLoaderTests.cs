using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup;
using NUnit.Framework;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Tests
{
    public sealed class RewardPickupViewLoaderTests
    {
        [Test]
        public async Task LoadAsync_DuplicateIds_LoadsDistinctViewsAndReleasesOnce()
        {
            var prefab = CreatePrefab();
            var resourceLoader = new FakeResourceLoader(prefab);
            var loader = new RewardPickupViewLoader(resourceLoader);
            RewardPickupViewSet views = null;
            try
            {
                views = await loader.LoadAsync(
                    new[]
                    {
                        "reward.gold",
                        "reward.crystal",
                        "reward.silver",
                        "reward.gold"
                    },
                    CancellationToken.None);

                Assert.That(resourceLoader.LoadCount, Is.EqualTo(3));
                Assert.That(views.Require("reward.gold"), Is.SameAs(prefab.GetComponent<RewardPickupView>()));
                Assert.That(views.Require("reward.crystal"), Is.SameAs(prefab.GetComponent<RewardPickupView>()));
                Assert.That(views.Require("reward.silver"), Is.SameAs(prefab.GetComponent<RewardPickupView>()));

                views.Dispose();
                views.Dispose();

                Assert.That(views.IsDisposed, Is.True);
                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(3));
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
            var loader = new RewardPickupViewLoader(resourceLoader);
            try
            {
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await loader.LoadAsync(new[] { "reward.gold" }, cancellation.Token));

                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        private static GameObject CreatePrefab()
        {
            var prefab = new GameObject("RewardPickupViewLoaderTestPrefab");
            prefab.AddComponent<RewardPickupView>();
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

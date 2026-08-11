using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor;
using NUnit.Framework;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Tests
{
    public sealed class ActorDefinitionLoaderTests
    {
        [Test]
        public void Catalog_WithDuplicateActorIds_Throws()
        {
            var definitions = new[]
            {
                Config("actor.king"),
                Config("actor.king")
            };

            var exception = Assert.Throws<ArgumentException>(
                () => new ActorConfigCatalog(definitions));

            StringAssert.Contains("configured more than once", exception.Message);
        }

        [Test]
        public void Catalog_RequireUnknownActorId_Throws()
        {
            var catalog = new ActorConfigCatalog(new[]
            {
                Config("actor.king")
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => catalog.Require("actor.unknown"));

            StringAssert.Contains("actor.unknown", exception.Message);
        }

        [Test]
        public void Catalog_ResolveDifferentLevels_ReturnsConfiguredStatsAndAttackRanks()
        {
            var catalog = new ActorConfigCatalog(new[]
            {
                new ActorDefinitionConfig(
                    "actor.king",
                    "KING",
                    "loadout.actor.king",
                    new[]
                    {
                        new ActorLevelDefinitionConfig(1, 100, 4f, 1),
                        new ActorLevelDefinitionConfig(2, 120, 4f, 2)
                    })
            });

            var first = catalog.Resolve("actor.king", 1);
            var second = catalog.Resolve("actor.king", 2);

            Assert.That(first.MaximumHealth, Is.EqualTo(100));
            Assert.That(first.PrimaryAttackRank, Is.EqualTo(1));
            Assert.That(second.MaximumHealth, Is.EqualTo(120));
            Assert.That(second.PrimaryAttackRank, Is.EqualTo(2));
        }

        [Test]
        public async Task LoadAsync_DuplicateRequests_LoadsDistinctActorsAndReleasesOnce()
        {
            var prefabObject = new GameObject("ActorDefinitionTestPrefab");
            prefabObject.AddComponent<ActorView>();
            var resourceLoader = new FakeResourceLoader(prefabObject);
            var loader = new ActorDefinitionLoader(
                new ActorConfigCatalog(new[]
                {
                    Config("actor.king", maximumHealth: 10),
                    Config("actor.druid", maximumHealth: 20)
                }),
                resourceLoader);

            ActorDefinitionSet definitions = null;
            try
            {
                definitions = await loader.LoadAsync(
                    new[] { "actor.king", "actor.druid", "actor.king" },
                    CancellationToken.None);

                Assert.That(resourceLoader.LoadCount, Is.EqualTo(2));
                Assert.That(definitions.Require("actor.king").ActorId, Is.EqualTo("actor.king"));
                Assert.That(definitions.Require("actor.druid").ActorId, Is.EqualTo("actor.druid"));

                definitions.Dispose();
                definitions.Dispose();

                Assert.That(definitions.IsDisposed, Is.True);
                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(2));
            }
            finally
            {
                definitions?.Dispose();
                UnityEngine.Object.DestroyImmediate(prefabObject);
            }
        }

        [Test]
        public void LoadAsync_CancelledAfterResourceReturns_ReleasesLoadedResource()
        {
            var prefabObject = new GameObject("CancelledActorDefinitionTestPrefab");
            prefabObject.AddComponent<ActorView>();
            using var cancellation = new CancellationTokenSource();
            var resourceLoader = new FakeResourceLoader(prefabObject, cancellation);
            var loader = new ActorDefinitionLoader(
                new ActorConfigCatalog(new[]
                {
                    Config("actor.king")
                }),
                resourceLoader);

            try
            {
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await loader.LoadAsync(
                        new[] { "actor.king" },
                        cancellation.Token));

                Assert.That(resourceLoader.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
            }
        }

        private static ActorDefinitionConfig Config(
            string actorId,
            int maximumHealth = 10)
        {
            return new ActorDefinitionConfig(
                actorId,
                actorId,
                "loadout.test",
                new[]
                {
                    new ActorLevelDefinitionConfig(
                        1,
                        maximumHealth,
                        movementSpeed: 3f,
                        primaryAttackRank: 1)
                });
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

            public Task PreloadInCacheAsync<TResource>(
                string resourceId,
                CancellationToken token)
            {
                throw new NotSupportedException();
            }

            public TResource LoadResource<TResource>(string resourceId)
            {
                throw new NotSupportedException();
            }

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token)
            {
                throw new NotSupportedException();
            }

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
                CancellationToken token = default)
            {
                throw new NotSupportedException();
            }

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

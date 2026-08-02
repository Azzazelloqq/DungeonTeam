using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    public sealed class DungeonFactory : IDungeonFactory
    {
        private readonly DungeonConfigPage _config;
        private readonly DungeonContentPlanner _contentPlanner = new DungeonContentPlanner();
        private readonly DungeonChunkLayoutPlanner _layoutPlanner = new DungeonChunkLayoutPlanner();

        public DungeonFactory(DungeonConfigPage config)
        {
            _config = config != null
                ? config
                : throw new ArgumentNullException(nameof(config));
        }

        public async UniTask<IDungeonInstance> CreateAsync(
            DungeonBuildRequest request,
            CancellationToken ownerToken)
        {
            var buildData = ResolveBuildData(request);
            return buildData.Kind == DungeonKind.Authored
                ? await CreateAuthoredAsync(request, buildData, ownerToken)
                : await CreateChunkedAsync(request, buildData, ownerToken);
        }

        private async UniTask<IDungeonInstance> CreateAuthoredAsync(
            DungeonBuildRequest request,
            BuildData buildData,
            CancellationToken ownerToken)
        {
            ownerToken.ThrowIfCancellationRequested();
            var handle = StartInstantiate(buildData.AuthoredMapAddress, Vector3.zero,
                Quaternion.identity, parent: null);
            var ownershipTransferred = false;
            try
            {
                var mapRoot = await AwaitInstanceAsync(
                    handle,
                    ownerToken,
                    "Failed to instantiate the authored dungeon map.");
                ownerToken.ThrowIfCancellationRequested();
                try
                {
                    var mapData = DungeonAuthoringReader.Read(
                        mapRoot,
                        request.DungeonId,
                        request.Seed);
                    var contentPlan = _contentPlanner.Build(
                        request.Seed,
                        mapData.EnemyPlacements,
                        mapData.InterestPointPlacements,
                        mapData.ObjectivePlacements,
                        buildData.Scenario,
                        buildData.Difficulty);
                    var instance = new DungeonInstance(
                        mapRoot,
                        mapData.Snapshot,
                        contentPlan);

                    ownershipTransferred = true;
                    return instance;
                }
                catch (Exception exception) when (exception is not DungeonBuildException)
                {
                    throw new DungeonBuildException(
                        DungeonBuildFailureReason.InvalidAuthoring,
                        $"Authored dungeon '{request.DungeonId}' is invalid.",
                        exception);
                }
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    ReleaseInstance(handle);
                }
            }
        }

        private async UniTask<IDungeonInstance> CreateChunkedAsync(
            DungeonBuildRequest request,
            BuildData buildData,
            CancellationToken ownerToken)
        {
            var assetHandles = new List<AsyncOperationHandle<GameObject>>(
                buildData.ChunkAddresses.Count);
            var metadata = new Dictionary<string, DungeonChunkMetadata>(StringComparer.Ordinal);
            var chunkInstances = new List<GameObject>(buildData.ChunkDefinition.TargetChunkCount);
            GameObject mapRoot = null;

            try
            {
                foreach (var pair in buildData.ChunkAddresses)
                {
                    ownerToken.ThrowIfCancellationRequested();
                    var handle = StartLoad(pair.Value);
                    assetHandles.Add(handle);
                    var prefab = await AwaitAssetAsync(handle, ownerToken, pair.Value);
                    try
                    {
                        metadata.Add(
                            pair.Key,
                            DungeonChunkAuthoringReader.ReadMetadata(prefab, pair.Key));
                    }
                    catch (Exception exception)
                    {
                        throw new DungeonBuildException(
                            DungeonBuildFailureReason.InvalidAuthoring,
                            $"Dungeon chunk '{pair.Key}' has invalid authoring.",
                            exception);
                    }
                }

                var layout = BuildLayout(request, buildData, metadata);
                mapRoot = new GameObject($"ChunkedDungeon_{request.DungeonId}");
                for (var index = 0; index < layout.Placements.Count; index++)
                {
                    ownerToken.ThrowIfCancellationRequested();
                    var placement = layout.Placements[index];
                    var address = buildData.ChunkAddresses[placement.ChunkId];
                    var handle = StartInstantiate(
                        address,
                        new Vector3(placement.X, 0f, placement.Z),
                        Quaternion.Euler(0f, placement.RotationQuarterTurns * 90f, 0f),
                        mapRoot.transform);
                    var ownershipTransferred = false;
                    try
                    {
                        var chunkInstance = await AwaitInstanceAsync(
                            handle,
                            ownerToken,
                            $"Failed to instantiate dungeon chunk '{placement.ChunkId}'.");
                        ownerToken.ThrowIfCancellationRequested();
                        chunkInstances.Add(chunkInstance);
                        ownershipTransferred = true;
                    }
                    finally
                    {
                        if (!ownershipTransferred)
                        {
                            ReleaseInstance(handle);
                        }
                    }
                }

                ownerToken.ThrowIfCancellationRequested();
                var instanceData = ReadChunkInstances(
                    request,
                    buildData,
                    chunkInstances);
                var instance = new DungeonInstance(
                    mapRoot,
                    chunkInstances.ToArray(),
                    instanceData.Snapshot,
                    instanceData.ContentPlan);
                mapRoot = null;
                chunkInstances.Clear();
                return instance;
            }
            finally
            {
                for (var index = chunkInstances.Count - 1; index >= 0; index--)
                {
                    if (chunkInstances[index] != null)
                    {
                        Addressables.ReleaseInstance(chunkInstances[index]);
                    }
                }

                if (mapRoot != null)
                {
                    UnityEngine.Object.Destroy(mapRoot);
                }

                for (var index = assetHandles.Count - 1; index >= 0; index--)
                {
                    if (assetHandles[index].IsValid())
                    {
                        Addressables.Release(assetHandles[index]);
                    }
                }
            }
        }

        private DungeonChunkLayout BuildLayout(
            DungeonBuildRequest request,
            BuildData buildData,
            IReadOnlyDictionary<string, DungeonChunkMetadata> metadata)
        {
            var definition = buildData.ChunkDefinition;
            var mandatory = ResolveMetadata(definition.MandatoryChunkIds, metadata);
            var pool = ResolveMetadata(definition.ChunkPool, metadata);
            try
            {
                return _layoutPlanner.Build(
                    request.Seed,
                    metadata[definition.EntryChunkId],
                    mandatory,
                    pool,
                    metadata[definition.ExitChunkId],
                    definition.TargetChunkCount,
                    definition.MaxGenerationAttempts);
            }
            catch (DungeonLayoutGenerationException exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.GenerationFailed,
                    $"Chunked dungeon '{request.DungeonId}' could not be generated.",
                    exception);
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.InvalidConfig,
                    $"Chunked dungeon '{request.DungeonId}' has invalid layout config.",
                    exception);
            }
        }

        private ChunkInstanceData ReadChunkInstances(
            DungeonBuildRequest request,
            BuildData buildData,
            IReadOnlyList<GameObject> chunkInstances)
        {
            try
            {
                var enemies = new List<EnemyPlacement>();
                var interestPoints = new List<InterestPointPlacement>();
                var objectives = new List<ObjectivePlacement>();
                for (var index = 0; index < chunkInstances.Count; index++)
                {
                    var placements = DungeonChunkAuthoringReader.ReadPlacements(
                        chunkInstances[index],
                        $"chunk{index}");
                    enemies.AddRange(placements.EnemyPlacements);
                    interestPoints.AddRange(placements.InterestPointPlacements);
                    objectives.AddRange(placements.ObjectivePlacements);
                }

                var snapshot = new DungeonMapSnapshot(
                    request.DungeonId,
                    request.Seed,
                    DungeonChunkAuthoringReader.RequireEntryPose(chunkInstances[0]),
                    DungeonChunkAuthoringReader.RequireExitPose(
                        chunkInstances[chunkInstances.Count - 1]));
                var contentPlan = _contentPlanner.Build(
                    request.Seed,
                    enemies,
                    interestPoints,
                    objectives,
                    buildData.Scenario,
                    buildData.Difficulty);
                return new ChunkInstanceData(snapshot, contentPlan);
            }
            catch (Exception exception) when (exception is not DungeonBuildException)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.InvalidAuthoring,
                    $"Chunked dungeon '{request.DungeonId}' instances have invalid authoring.",
                    exception);
            }
        }

        private BuildData ResolveBuildData(DungeonBuildRequest request)
        {
            try
            {
                var scenario = _config.RequireScenario(request.ScenarioId).ToDomain();
                var difficulty = _config.RequireDifficulty(request.DifficultyId).ToDomain();
                var authored = _config.TryGetAuthoredDungeon(request.DungeonId);
                var chunked = _config.TryGetChunkedDungeon(request.DungeonId);
                if (authored != null && chunked != null)
                {
                    throw new InvalidOperationException(
                        $"Dungeon ID '{request.DungeonId}' is configured more than once.");
                }

                if (authored != null)
                {
                    return BuildData.ForAuthored(
                        DungeonMapAssetCatalog.ResolveAddress(authored.MapAssetId),
                        scenario,
                        difficulty);
                }

                if (chunked != null)
                {
                    return BuildData.ForChunks(
                        chunked,
                        ResolveChunkAddresses(chunked),
                        scenario,
                        difficulty);
                }

                throw new InvalidOperationException(
                    $"Dungeon config '{_config.name}' does not contain dungeon ID " +
                    $"'{request.DungeonId}'.");
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.InvalidConfig,
                    $"Dungeon build request for '{request.DungeonId}' has invalid config.",
                    exception);
            }
        }

        private static Dictionary<string, string> ResolveChunkAddresses(
            ChunkedDungeonDefinition definition)
        {
            if (definition.MandatoryChunkIds == null || definition.ChunkPool == null)
            {
                throw new InvalidOperationException("Dungeon chunk ID arrays cannot be null.");
            }

            var addresses = new Dictionary<string, string>(StringComparer.Ordinal);
            AddChunkAddress(addresses, definition.EntryChunkId);
            for (var index = 0; index < definition.MandatoryChunkIds.Length; index++)
            {
                AddChunkAddress(addresses, definition.MandatoryChunkIds[index]);
            }

            for (var index = 0; index < definition.ChunkPool.Length; index++)
            {
                AddChunkAddress(addresses, definition.ChunkPool[index]);
            }

            AddChunkAddress(addresses, definition.ExitChunkId);
            return addresses;
        }

        private static void AddChunkAddress(IDictionary<string, string> addresses, string chunkId)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                throw new InvalidOperationException("Dungeon chunk ID cannot be empty.");
            }

            if (!addresses.ContainsKey(chunkId))
            {
                addresses.Add(chunkId, DungeonChunkAssetCatalog.ResolveAddress(chunkId));
            }
        }

        private static DungeonChunkMetadata[] ResolveMetadata(
            string[] chunkIds,
            IReadOnlyDictionary<string, DungeonChunkMetadata> metadata)
        {
            var result = new DungeonChunkMetadata[chunkIds.Length];
            for (var index = 0; index < chunkIds.Length; index++)
            {
                result[index] = metadata[chunkIds[index]];
            }

            return result;
        }

        private static AsyncOperationHandle<GameObject> StartLoad(string address)
        {
            try
            {
                return Addressables.LoadAssetAsync<GameObject>(address);
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    $"Failed to start loading dungeon asset '{address}'.",
                    exception);
            }
        }

        private static AsyncOperationHandle<GameObject> StartInstantiate(
            string address,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            try
            {
                return Addressables.InstantiateAsync(address, position, rotation, parent);
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    $"Failed to start instantiating dungeon asset '{address}'.",
                    exception);
            }
        }

        private static async UniTask<GameObject> AwaitAssetAsync(
            AsyncOperationHandle<GameObject> handle,
            CancellationToken token,
            string address)
        {
            try
            {
                return await handle.ToUniTask(cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                await AwaitOperationCompletion(handle);
                throw;
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    $"Failed to load dungeon asset '{address}'.",
                    exception);
            }
        }

        private static async UniTask<GameObject> AwaitInstanceAsync(
            AsyncOperationHandle<GameObject> handle,
            CancellationToken token,
            string errorMessage)
        {
            try
            {
                return await handle.ToUniTask(cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                await AwaitOperationCompletion(handle);
                throw;
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    errorMessage,
                    exception);
            }
        }

        private static async UniTask AwaitOperationCompletion(
            AsyncOperationHandle<GameObject> handle)
        {
            try
            {
                await handle.Task;
            }
            catch
            {
            }
        }

        private static void ReleaseInstance(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                Addressables.ReleaseInstance(handle.Result);
            }
            else
            {
                Addressables.Release(handle);
            }
        }

        private enum DungeonKind
        {
            Authored,
            Chunked
        }

        private sealed class BuildData
        {
            private BuildData(
                DungeonKind kind,
                string authoredMapAddress,
                ChunkedDungeonDefinition chunkDefinition,
                Dictionary<string, string> chunkAddresses,
                DungeonScenario scenario,
                DungeonDifficulty difficulty)
            {
                Kind = kind;
                AuthoredMapAddress = authoredMapAddress;
                ChunkDefinition = chunkDefinition;
                ChunkAddresses = chunkAddresses;
                Scenario = scenario;
                Difficulty = difficulty;
            }

            public DungeonKind Kind { get; }
            public string AuthoredMapAddress { get; }
            public ChunkedDungeonDefinition ChunkDefinition { get; }
            public Dictionary<string, string> ChunkAddresses { get; }
            public DungeonScenario Scenario { get; }
            public DungeonDifficulty Difficulty { get; }

            public static BuildData ForAuthored(
                string mapAddress,
                DungeonScenario scenario,
                DungeonDifficulty difficulty)
            {
                return new BuildData(
                    DungeonKind.Authored,
                    mapAddress,
                    null,
                    null,
                    scenario,
                    difficulty);
            }

            public static BuildData ForChunks(
                ChunkedDungeonDefinition definition,
                Dictionary<string, string> addresses,
                DungeonScenario scenario,
                DungeonDifficulty difficulty)
            {
                return new BuildData(
                    DungeonKind.Chunked,
                    null,
                    definition,
                    addresses,
                    scenario,
                    difficulty);
            }
        }

        private readonly struct ChunkInstanceData
        {
            public ChunkInstanceData(
                DungeonMapSnapshot snapshot,
                DungeonContentPlan contentPlan)
            {
                Snapshot = snapshot;
                ContentPlan = contentPlan;
            }

            public DungeonMapSnapshot Snapshot { get; }
            public DungeonContentPlan ContentPlan { get; }
        }
    }
}

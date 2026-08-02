using System;
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
            ownerToken.ThrowIfCancellationRequested();

            AsyncOperationHandle<GameObject> handle;
            try
            {
                handle = Addressables.InstantiateAsync(buildData.MapAddress);
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    $"Failed to start loading dungeon map '{buildData.MapAddress}'.",
                    exception);
            }

            var ownershipTransferred = false;
            try
            {
                var mapRoot = await AwaitInstanceAsync(handle, ownerToken);
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
                    Release(handle);
                }
            }
        }

        private BuildData ResolveBuildData(DungeonBuildRequest request)
        {
            try
            {
                var dungeon = _config.RequireAuthoredDungeon(request.DungeonId);
                var scenario = _config.RequireScenario(request.ScenarioId).ToDomain();
                var difficulty = _config.RequireDifficulty(request.DifficultyId).ToDomain();
                var mapAddress = DungeonMapAssetCatalog.ResolveAddress(dungeon.MapAssetId);
                return new BuildData(mapAddress, scenario, difficulty);
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.InvalidConfig,
                    $"Dungeon build request for '{request.DungeonId}' has invalid config.",
                    exception);
            }
        }

        private static async UniTask<GameObject> AwaitInstanceAsync(
            AsyncOperationHandle<GameObject> handle,
            CancellationToken token)
        {
            try
            {
                return await handle.ToUniTask(cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                await WaitForCompletionSilently(handle);
                throw;
            }
            catch (Exception exception)
            {
                throw new DungeonBuildException(
                    DungeonBuildFailureReason.MissingAsset,
                    "Failed to instantiate the authored dungeon map.",
                    exception);
            }
        }

        private static async UniTask WaitForCompletionSilently(
            AsyncOperationHandle<GameObject> handle)
        {
            try
            {
                await handle.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private static void Release(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                Addressables.ReleaseInstance(handle.Result);
                return;
            }

            Addressables.Release(handle);
        }

        private readonly struct BuildData
        {
            public BuildData(
                string mapAddress,
                DungeonScenario scenario,
                DungeonDifficulty difficulty)
            {
                MapAddress = mapAddress;
                Scenario = scenario;
                Difficulty = difficulty;
            }

            public string MapAddress { get; }
            public DungeonScenario Scenario { get; }
            public DungeonDifficulty Difficulty { get; }
        }
    }
}

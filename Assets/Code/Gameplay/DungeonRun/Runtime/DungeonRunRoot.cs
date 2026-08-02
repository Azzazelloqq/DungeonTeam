using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.Team.Runtime;
using RootPattern;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class DungeonRunRoot : Root
    {
        private readonly IDungeonFactory _dungeonFactory;
        private readonly DungeonBuildRequest _buildRequest;
        private readonly DungeonRunBindings _bindings;
        private readonly Camera _worldCamera;
        private readonly ITickHandler _tickHandler;
        private readonly TeamControlSettings _teamControlSettings;
        private readonly List<ActorInstance> _enemies = new();

        private ITeamInput _teamInput;
        private TeamController _teamController;
        private IDungeonInstance _dungeonInstance;
        private DungeonRunNavigation _navigation;
        private GameObject _actorsRoot;

        public DungeonRunRoot(
            IDungeonFactory dungeonFactory,
            DungeonBuildRequest buildRequest,
            DungeonRunBindings bindings,
            Camera worldCamera,
            ITickHandler tickHandler,
            ITeamInput teamInput,
            TeamControlSettings teamControlSettings)
        {
            _dungeonFactory = dungeonFactory ?? throw new ArgumentNullException(nameof(dungeonFactory));
            _buildRequest = buildRequest;
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _worldCamera = worldCamera != null
                ? worldCamera
                : throw new ArgumentNullException(nameof(worldCamera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _teamInput = teamInput ?? throw new ArgumentNullException(nameof(teamInput));
            _teamControlSettings = teamControlSettings ??
                throw new ArgumentNullException(nameof(teamControlSettings));
        }

        public DungeonMapSnapshot MapSnapshot => RequireDungeon().MapSnapshot;

        public DungeonContentPlan ContentPlan => RequireDungeon().ContentPlan;

        public ActorInstance Leader { get; private set; }

        public ActorInstance Companion { get; private set; }

        public IReadOnlyList<ActorInstance> Enemies => _enemies;

        public int EnemyCount => _enemies.Count;

        protected override async UniTask OnInitializeAsync(CancellationToken token)
        {
            _bindings.Validate();

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                CancellationToken);
            var ownerToken = linkedTokenSource.Token;

            _dungeonInstance = await _dungeonFactory.CreateAsync(_buildRequest, ownerToken);
            ownerToken.ThrowIfCancellationRequested();

            _navigation = new DungeonRunNavigation();
            _navigation.Build();
            ownerToken.ThrowIfCancellationRequested();

            _actorsRoot = new GameObject("DungeonRunActors");
            CreateActors();

            _teamController = new TeamController(
                Leader,
                Companion,
                _worldCamera,
                _tickHandler,
                _teamInput,
                _teamControlSettings);
            _teamInput = null;
            _teamController.Initialize();
        }

        protected override void OnDispose()
        {
            _teamController?.Dispose();
            _teamController = null;

            _teamInput?.Dispose();
            _teamInput = null;

            for (var index = _enemies.Count - 1; index >= 0; index--)
            {
                _enemies[index].Dispose();
            }

            _enemies.Clear();

            Companion?.Dispose();
            Companion = null;

            Leader?.Dispose();
            Leader = null;

            if (_actorsRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_actorsRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_actorsRoot);
                }

                _actorsRoot = null;
            }

            _navigation?.Dispose();
            _navigation = null;

            _dungeonInstance?.Dispose();
            _dungeonInstance = null;
        }

        private void CreateActors()
        {
            var actorFactory = new ActorFactory();
            var entryPose = MapSnapshot.EntryPose;
            var entryPosition = DungeonPoseConversion.ToPosition(entryPose);
            var entryRotation = DungeonPoseConversion.ToRotation(entryPose);

            Leader = CreateActor(
                actorFactory,
                "Leader",
                _navigation.RequireSpawnPosition(entryPosition),
                entryRotation,
                _bindings.Leader);

            Companion = CreateActor(
                actorFactory,
                "Companion",
                _navigation.RequireSpawnPosition(entryPosition + _bindings.CompanionOffset),
                entryRotation,
                _bindings.Companion);

            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                var enemySpawn = ContentPlan.EnemySpawns[index];
                var enemy = CreateActor(
                    actorFactory,
                    $"Enemy_{enemySpawn.PlacementId}",
                    _navigation.RequireSpawnPosition(
                        DungeonPoseConversion.ToPosition(enemySpawn.Pose)),
                    DungeonPoseConversion.ToRotation(enemySpawn.Pose),
                    _bindings.Enemy);
                _enemies.Add(enemy);
            }
        }

        private ActorInstance CreateActor(
            ActorFactory actorFactory,
            string instanceName,
            Vector3 position,
            Quaternion rotation,
            GreyboxActorSettings settings)
        {
            return actorFactory.Create(
                _bindings.ActorPrefab,
                new ActorSpawnRequest(
                    instanceName,
                    position,
                    rotation,
                    settings.MaximumHealth,
                    settings.MovementSpeed,
                    settings.Color),
                _actorsRoot.transform);
        }

        private IDungeonInstance RequireDungeon()
        {
            return _dungeonInstance ?? throw new InvalidOperationException(
                "Dungeon Run is not initialized.");
        }
    }
}

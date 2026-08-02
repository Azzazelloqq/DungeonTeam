using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
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
        private readonly RectTransform _contextActionsParent;
        private readonly Camera _worldCamera;
        private readonly ITickHandler _tickHandler;
        private readonly TeamControlSettings _teamControlSettings;
        private readonly EnemyAiSettings _enemyAiSettings;
        private readonly List<ActorInstance> _enemies = new();
        private readonly List<EnemyAiController> _enemyAiControllers = new();

        private ITeamInput _teamInput;
        private TeamController _teamController;
        private DungeonRunContextActionsController _contextActionsController;
        private ContextActionsViewModel _contextActionsViewModel;
        private ContextActionsViewBase _contextActionsView;
        private IDungeonInstance _dungeonInstance;
        private DungeonRunNavigation _navigation;
        private GameObject _actorsRoot;

        public DungeonRunRoot(
            IDungeonFactory dungeonFactory,
            DungeonBuildRequest buildRequest,
            DungeonRunBindings bindings,
            RectTransform contextActionsParent,
            Camera worldCamera,
            ITickHandler tickHandler,
            ITeamInput teamInput,
            TeamControlSettings teamControlSettings,
            EnemyAiSettings enemyAiSettings)
        {
            _dungeonFactory = dungeonFactory ?? throw new ArgumentNullException(nameof(dungeonFactory));
            _buildRequest = buildRequest;
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _contextActionsParent = contextActionsParent != null
                ? contextActionsParent
                : throw new ArgumentNullException(nameof(contextActionsParent));
            _worldCamera = worldCamera != null
                ? worldCamera
                : throw new ArgumentNullException(nameof(worldCamera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _teamInput = teamInput ?? throw new ArgumentNullException(nameof(teamInput));
            _teamControlSettings = teamControlSettings ??
                throw new ArgumentNullException(nameof(teamControlSettings));
            _enemyAiSettings = enemyAiSettings ??
                throw new ArgumentNullException(nameof(enemyAiSettings));
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
                _enemies,
                _worldCamera,
                _tickHandler,
                _teamInput,
                _teamControlSettings);
            _teamInput = null;
            _teamController.Initialize();

            CreateEnemyAiControllers();
            CreateContextActions();
        }

        protected override void OnDispose()
        {
            _contextActionsController?.Dispose();
            _contextActionsController = null;

            _contextActionsView?.Dispose();
            _contextActionsView = null;

            _contextActionsViewModel?.Dispose();
            _contextActionsViewModel = null;

            for (var index = _enemyAiControllers.Count - 1; index >= 0; index--)
            {
                _enemyAiControllers[index].Dispose();
            }

            _enemyAiControllers.Clear();

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

        private void CreateEnemyAiControllers()
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                var controller = new EnemyAiController(
                    _enemies[index],
                    Leader,
                    Companion,
                    _tickHandler,
                    _enemyAiSettings);
                _enemyAiControllers.Add(controller);
                controller.Initialize();
            }
        }

        private void CreateContextActions()
        {
            _contextActionsView = UnityEngine.Object.Instantiate(
                _bindings.ContextActionsPrefab,
                _contextActionsParent,
                worldPositionStays: false);
            _contextActionsView.name = "ContextActions";
            _contextActionsView.gameObject.SetActive(true);

            var model = new ContextActionsModel();
            _contextActionsViewModel = new ContextActionsViewModel(model);
            _contextActionsViewModel.Initialize();
            _contextActionsView.Initialize(
                _contextActionsViewModel,
                disposeWithViewModel: false);

            _contextActionsController = new DungeonRunContextActionsController(
                _teamController,
                model);
            _contextActionsController.Initialize();
        }

        private IDungeonInstance RequireDungeon()
        {
            return _dungeonInstance ?? throw new InvalidOperationException(
                "Dungeon Run is not initialized.");
        }
    }
}

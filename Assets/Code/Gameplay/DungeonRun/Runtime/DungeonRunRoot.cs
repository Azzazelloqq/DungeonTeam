using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Chests.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Domain;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime.Presentation.Gameplay.DungeonCamera;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.UI.CombatHud;
using DungeonTeam.UI.CombatHud.Base;
using RootPattern;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public sealed class DungeonRunRoot : Root
    {
        private readonly IDungeonFactory _dungeonFactory;
        private readonly DungeonRunStartRequest _startRequest;
        private readonly DungeonRunBindings _bindings;
        private readonly IActorDefinitionLoader _actorDefinitionLoader;
        private readonly ActorConfigCatalog _actorCatalog;
        private readonly SkillCatalog _skillCatalog;
        private readonly ISkillViewLoader _skillViewLoader;
        private readonly IRewardPickupViewLoader _rewardPickupViewLoader;
        private readonly IChestViewLoader _chestViewLoader;
        private readonly RectTransform _contextActionsParent;
        private readonly Camera _worldCamera;
        private readonly ITickHandler _tickHandler;
        private readonly RewardCatalog _rewardCatalog;
        private readonly TeamControlSettings _teamControlSettings;
        private readonly HeroControlSettings _heroControlSettings;
        private readonly EnemyBehaviorCatalog _enemyBehaviorCatalog;
        private readonly string _runId = Guid.NewGuid().ToString("N");
        private readonly RewardPickupFactory _rewardPickupFactory = new();
        private readonly ChestFactory _chestFactory = new();
        private readonly List<ActorInstance> _heroes = new();
        private readonly List<ActorInstance> _companions = new();
        private readonly List<Vector3> _companionFormationOffsets = new();
        private readonly List<ActorInstance> _enemies = new();
        private readonly List<ActorCombatController> _companionCombatControllers = new();
        private readonly List<ActorCombatController> _enemyCombatControllers = new();
        private readonly List<EnemyAiController> _enemyAiControllers = new();
        private readonly Dictionary<ActorInstance, EnemySpawnPlan> _enemySpawnPlans = new();
        private readonly List<RewardPickupInstance> _rewardPickups = new();
        private readonly List<ChestInstance> _chests = new();
        private readonly List<IReadOnlyList<DungeonRewardGrantPlan>> _chestRewardPlans = new();

        private IDungeonRunInput _input;
        private VirtualHeroInput _virtualHeroInput;
        private CompositeHeroInput _heroInput;
        private ActorDefinitionSet _actorDefinitions;
        private RewardPickupViewSet _rewardPickupViews;
        private ChestViewSet _chestViews;
        private SkillViewSet _skillViews;
        private SkillExecutionController _skillExecution;
        private HeroController _heroController;
        private ActorCombatController _leaderCombatController;
        private TeamController _teamController;
        private DungeonCameraPresenter _cameraPresenter;
        private WallOcclusionController _wallOcclusionController;
        private DungeonRunContextActionsController _contextActionsController;
        private ContextActionsViewModel _contextActionsViewModel;
        private ContextActionsViewBase _contextActionsView;
        private DungeonRunCombatHudController _combatHudController;
        private CombatHudViewModel _combatHudViewModel;
        private CombatHudViewBase _combatHudView;
        private DungeonRunProgress _progress;
        private DungeonRunRouteController _routeController;
        private DungeonRunVisibilityController _visibilityController;
        private IDungeonInstance _dungeonInstance;
        private DungeonRunNavigation _navigation;
        private Vector3 _exitPosition;
        private GameObject _actorsRoot;
        private GameObject _skillProjectilesRoot;
        private GameObject _rewardsRoot;
        private GameObject _interestsRoot;
        private bool _encounterActivated;
        private bool _enemyAiStartScheduled;
        private bool _chestsCreated;
        private bool _isDisposed;

        public event Action ProgressChanged;

        public event Action<DungeonRunResult> Finished;

        public DungeonRunRoot(
            IDungeonFactory dungeonFactory,
            DungeonRunStartRequest startRequest,
            DungeonRunBindings bindings,
            IActorDefinitionLoader actorDefinitionLoader,
            ActorConfigCatalog actorCatalog,
            SkillCatalog skillCatalog,
            ISkillViewLoader skillViewLoader,
            IRewardPickupViewLoader rewardPickupViewLoader,
            IChestViewLoader chestViewLoader,
            RectTransform contextActionsParent,
            Camera worldCamera,
            ITickHandler tickHandler,
            IDungeonRunInput input,
            RewardCatalog rewardCatalog,
            TeamControlSettings teamControlSettings,
            HeroControlSettings heroControlSettings,
            EnemyBehaviorCatalog enemyBehaviorCatalog)
        {
            _dungeonFactory = dungeonFactory ?? throw new ArgumentNullException(nameof(dungeonFactory));
            _startRequest = startRequest ?? throw new ArgumentNullException(nameof(startRequest));
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _actorDefinitionLoader = actorDefinitionLoader ??
                throw new ArgumentNullException(nameof(actorDefinitionLoader));
            _actorCatalog = actorCatalog ?? throw new ArgumentNullException(nameof(actorCatalog));
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _skillViewLoader = skillViewLoader ??
                throw new ArgumentNullException(nameof(skillViewLoader));
            _rewardPickupViewLoader = rewardPickupViewLoader ??
                throw new ArgumentNullException(nameof(rewardPickupViewLoader));
            _chestViewLoader = chestViewLoader ??
                throw new ArgumentNullException(nameof(chestViewLoader));
            _contextActionsParent = contextActionsParent != null
                ? contextActionsParent
                : throw new ArgumentNullException(nameof(contextActionsParent));
            _worldCamera = worldCamera != null
                ? worldCamera
                : throw new ArgumentNullException(nameof(worldCamera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rewardCatalog = rewardCatalog ?? throw new ArgumentNullException(nameof(rewardCatalog));
            _teamControlSettings = teamControlSettings ??
                throw new ArgumentNullException(nameof(teamControlSettings));
            _heroControlSettings = heroControlSettings ??
                throw new ArgumentNullException(nameof(heroControlSettings));
            _enemyBehaviorCatalog = enemyBehaviorCatalog ??
                throw new ArgumentNullException(nameof(enemyBehaviorCatalog));
        }

        public DungeonMapSnapshot MapSnapshot => RequireDungeon().MapSnapshot;

        public DungeonContentPlan ContentPlan => RequireDungeon().ContentPlan;

        public ActorInstance Leader { get; private set; }

        public IReadOnlyList<ActorInstance> Companions => _companions;

        public IReadOnlyList<ActorInstance> Heroes => _heroes;

        public IReadOnlyList<ActorInstance> Enemies => _enemies;

        public IReadOnlyList<ChestInstance> Chests => _chests;

        public int EnemyCount => _dungeonInstance == null
            ? 0
            : ContentPlan.EnemySpawns.Count;

        public int RewardPickupCount => _rewardPickups.Count;

        public int KilledEnemyCount => _progress?.KilledEnemies ?? 0;

        public int CollectedRewardCount => _progress?.CollectedRewardCount ?? 0;

        public bool CanExit => _progress?.CanExit ?? false;

        public bool IsFinished => _progress?.IsFinished ?? false;

        protected override async UniTask OnInitializeAsync(CancellationToken token)
        {
            _bindings.Validate();

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                CancellationToken);
            var ownerToken = linkedTokenSource.Token;

            _dungeonInstance = await _dungeonFactory.CreateAsync(
                _startRequest.Dungeon,
                ownerToken);
            ownerToken.ThrowIfCancellationRequested();
            _visibilityController = new DungeonRunVisibilityController(
                MapSnapshot.VisibilityLayout,
                _dungeonInstance.VisibilityBinding);
            ValidateRewardPlans();
            ValidateEnemyBehaviors();
            ValidateLoadouts();

            _navigation = new DungeonRunNavigation();
            _navigation.Build();
            ownerToken.ThrowIfCancellationRequested();

            _actorDefinitions = await _actorDefinitionLoader.LoadAsync(
                GetRequiredActorIds(),
                ownerToken);
            ownerToken.ThrowIfCancellationRequested();
            _skillViews = await _skillViewLoader.LoadAsync(
                GetRequiredLoadoutIds(),
                ownerToken);
            ownerToken.ThrowIfCancellationRequested();
            _rewardPickupViews = await _rewardPickupViewLoader.LoadAsync(
                GetRequiredRewardPickupIds(),
                ownerToken);
            ownerToken.ThrowIfCancellationRequested();
            _chestViews = await _chestViewLoader.LoadAsync(
                GetRequiredChestIds(),
                ownerToken);
            ownerToken.ThrowIfCancellationRequested();

            _actorsRoot = new GameObject("DungeonRunActors");
            _skillProjectilesRoot = new GameObject("DungeonRunSkillProjectiles");
            _skillExecution = new SkillExecutionController(
                _skillViews,
                _tickHandler,
                _skillProjectilesRoot.transform);
            CreateHeroes();
            var hasAuthoredRoute = MapSnapshot.SpatialLayout.HasAuthoredData;
            _progress = new DungeonRunProgress(
                ContentPlan.EnemySpawns.Count,
                requiresRouteCompletion: hasAuthoredRoute);
            if (!hasAuthoredRoute)
            {
                ActivateEncounter();
            }
            _exitPosition = _navigation.RequireSpawnPosition(
                DungeonPoseConversion.ToPosition(MapSnapshot.ExitPose));
            _rewardsRoot = new GameObject("DungeonRunRewards");
            _interestsRoot = new GameObject("DungeonRunInterests");
            if (!_visibilityController.IsEnabled)
            {
                CreateChests();
            }
            SubscribeToActorDeaths();

            _input.Enable();
            _virtualHeroInput = new VirtualHeroInput();
            _virtualHeroInput.Enable();
            _heroInput = new CompositeHeroInput(_input, _virtualHeroInput);

            _heroController = new HeroController(
                Leader,
                _heroes,
                _enemies,
                _worldCamera,
                _tickHandler,
                _heroInput,
                _heroControlSettings,
                _leaderCombatController,
                _navigation.HasCompletePath);
            _heroController.Initialize();

            _teamController = new TeamController(
                Leader,
                _companions,
                _companionCombatControllers,
                _companionFormationOffsets,
                _enemies,
                _tickHandler,
                _teamControlSettings);
            _teamController.Initialize();

            _cameraPresenter = new DungeonCameraPresenter(
                new DungeonCameraView(_worldCamera),
                new DungeonCameraModel(_bindings.CameraSettings, MapSnapshot.SpatialLayout),
                () => Leader.Position,
                _tickHandler);
            _cameraPresenter.Initialize();

            _wallOcclusionController = new WallOcclusionController(
                _worldCamera,
                _heroes,
                _tickHandler,
                _bindings);
            _wallOcclusionController.Initialize();

            _skillExecution.Initialize();
            if (hasAuthoredRoute)
            {
                CreateRouteController();
            }
            else
            {
                CreateEnemyAiControllers();
            }
            CreateCombatHud();
            CreateContextActions();
        }

        protected override void OnDispose()
        {
            _isDisposed = true;
            DisposeActiveControllers();
            DisposeSkillExecution();
            DisposeCombatControllers();

            DisposeContextActionsPresentation();
            DisposeCombatHudPresentation();

            ProgressChanged = null;
            Finished = null;

            for (var index = _chests.Count - 1; index >= 0; index--)
            {
                _chests[index].Dispose();
            }

            _chests.Clear();
            _chestRewardPlans.Clear();

            if (_interestsRoot != null)
            {
                DestroyGameObject(_interestsRoot);
                _interestsRoot = null;
            }

            for (var index = _rewardPickups.Count - 1; index >= 0; index--)
            {
                _rewardPickups[index].Dispose();
            }

            _rewardPickups.Clear();

            if (_rewardsRoot != null)
            {
                DestroyGameObject(_rewardsRoot);
                _rewardsRoot = null;
            }

            UnsubscribeFromActorDeaths();
            _progress = null;

            for (var index = _enemies.Count - 1; index >= 0; index--)
            {
                _enemies[index].Dispose();
            }

            _enemies.Clear();

            for (var index = _heroes.Count - 1; index >= 0; index--)
            {
                _heroes[index].Dispose();
            }

            Leader = null;
            _companions.Clear();
            _companionFormationOffsets.Clear();
            _heroes.Clear();
            _enemySpawnPlans.Clear();
            _encounterActivated = false;
            _enemyAiStartScheduled = false;

            if (_actorsRoot != null)
            {
                DestroyGameObject(_actorsRoot);
                _actorsRoot = null;
            }

            _actorDefinitions?.Dispose();
            _actorDefinitions = null;

            _skillViews?.Dispose();
            _skillViews = null;

            _rewardPickupViews?.Dispose();
            _rewardPickupViews = null;

            _chestViews?.Dispose();
            _chestViews = null;

            _navigation?.Dispose();
            _navigation = null;

            _dungeonInstance?.Dispose();
            _dungeonInstance = null;
            _visibilityController = null;
        }

        private void CreateHeroes()
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
                _startRequest.Team.Leader);
            _leaderCombatController = CreateCombatController(
                Leader,
                _startRequest.Team.Leader);
            _heroes.Add(Leader);

            for (var index = 0;
                 index < _startRequest.Team.Companions.Count;
                 index++)
            {
                var formationOffset = GetCompanionFormationOffset(index);
                var companion = CreateActor(
                    actorFactory,
                    $"Companion_{index + 1}",
                    _navigation.RequireSpawnPosition(
                        entryPosition + entryRotation * formationOffset),
                    entryRotation,
                    _startRequest.Team.Companions[index]);
                _companions.Add(companion);
                _companionCombatControllers.Add(CreateCombatController(
                    companion,
                    _startRequest.Team.Companions[index]));
                _companionFormationOffsets.Add(formationOffset);
                _heroes.Add(companion);
            }
        }

        private Vector3 GetCompanionFormationOffset(int companionIndex)
        {
            var spatialLayout = MapSnapshot.SpatialLayout;
            if (!spatialLayout.HasAuthoredData)
            {
                return _bindings.GetCompanionSpawnOffset(companionIndex);
            }

            if (companionIndex >= spatialLayout.CompanionFormationOffsets.Count)
            {
                throw new InvalidOperationException(
                    "Authored dungeon requires one formation offset per companion.");
            }

            var offset = spatialLayout.CompanionFormationOffsets[companionIndex];
            return new Vector3(offset.X, offset.Y, offset.Z);
        }

        private void ActivateEncounter()
        {
            if (_encounterActivated)
            {
                return;
            }

            _encounterActivated = true;
            var actorFactory = new ActorFactory();
            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                var enemySpawn = ContentPlan.EnemySpawns[index];
                var selection = new DungeonRunActorSelection(
                    enemySpawn.EnemyId,
                    enemySpawn.ActorLevel,
                    enemySpawn.LoadoutId);
                var enemy = CreateActor(
                    actorFactory,
                    $"Enemy_{enemySpawn.PlacementId}",
                    _navigation.RequireSpawnPosition(
                        DungeonPoseConversion.ToPosition(enemySpawn.Pose)),
                    DungeonPoseConversion.ToRotation(enemySpawn.Pose),
                    selection);
                _enemies.Add(enemy);
                _enemySpawnPlans.Add(enemy, enemySpawn);
                _enemyCombatControllers.Add(CreateCombatController(enemy, selection));
                enemy.Died += OnEnemyDied;
            }
        }

        private IReadOnlyList<string> GetRequiredActorIds()
        {
            var actorIds = new List<string>(
                ContentPlan.EnemySpawns.Count + _startRequest.Team.MemberCount)
            {
                _startRequest.Team.LeaderActorId
            };
            for (var index = 0;
                 index < _startRequest.Team.Companions.Count;
                 index++)
            {
                actorIds.Add(_startRequest.Team.Companions[index].ActorId);
            }

            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                actorIds.Add(ContentPlan.EnemySpawns[index].EnemyId);
            }

            return actorIds;
        }

        private IReadOnlyList<string> GetRequiredRewardPickupIds()
        {
            var rewardIds = new List<string>();
            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                AddRewardIds(rewardIds, ContentPlan.EnemySpawns[index].Rewards);
            }

            for (var index = 0; index < ContentPlan.InterestPointSpawns.Count; index++)
            {
                if (_chestViewLoader.Supports(
                        ContentPlan.InterestPointSpawns[index].InterestPointId))
                {
                    AddRewardIds(rewardIds, ContentPlan.InterestPointSpawns[index].Rewards);
                }
            }

            return rewardIds;
        }

        private IReadOnlyList<string> GetRequiredLoadoutIds()
        {
            var loadoutIds = new List<string>(
                ContentPlan.EnemySpawns.Count + _startRequest.Team.MemberCount)
            {
                _startRequest.Team.Leader.LoadoutId
            };
            for (var index = 0; index < _startRequest.Team.Companions.Count; index++)
            {
                loadoutIds.Add(_startRequest.Team.Companions[index].LoadoutId);
            }

            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                loadoutIds.Add(ContentPlan.EnemySpawns[index].LoadoutId);
            }

            return loadoutIds;
        }

        private IReadOnlyList<string> GetRequiredChestIds()
        {
            var chestIds = new List<string>();
            for (var index = 0; index < ContentPlan.InterestPointSpawns.Count; index++)
            {
                var interestPointId = ContentPlan.InterestPointSpawns[index].InterestPointId;
                if (_chestViewLoader.Supports(interestPointId))
                {
                    chestIds.Add(interestPointId);
                }
            }

            return chestIds;
        }

        private static void AddRewardIds(
            ICollection<string> target,
            IReadOnlyList<DungeonRewardGrantPlan> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                target.Add(rewards[index].RewardId);
            }
        }

        private ActorInstance CreateActor(
            ActorFactory actorFactory,
            string instanceName,
            Vector3 position,
            Quaternion rotation,
            DungeonRunActorSelection selection)
        {
            var runtimeDefinition = _actorCatalog.Resolve(
                selection.ActorId,
                selection.Level);
            runtimeDefinition = runtimeDefinition.WithBonuses(
                selection.Bonus.MaximumHealthBonus,
                selection.Bonus.MovementSpeedBonus);
            return actorFactory.Create(
                _actorDefinitions.Require(selection.ActorId),
                runtimeDefinition,
                new ActorSpawnRequest(
                    instanceName,
                    position,
                    rotation),
                _actorsRoot.transform);
        }

        private ActorCombatController CreateCombatController(
            ActorInstance actor,
            DungeonRunActorSelection selection)
        {
            return new ActorCombatController(
                actor,
                _skillCatalog,
                selection.LoadoutId,
                _skillExecution,
                selection.Bonus.PrimaryDamageBonus);
        }

        private void CreateEnemyAiControllers()
        {
            for (var index = _enemyAiControllers.Count; index < _enemies.Count; index++)
            {
                var behaviorId = _enemySpawnPlans[_enemies[index]].BehaviorId;
                var controller = new EnemyAiController(
                    _enemies[index],
                    _heroes,
                    _tickHandler,
                    _enemyBehaviorCatalog.Require(behaviorId),
                    _enemyCombatControllers[index]);
                _enemyAiControllers.Add(controller);
                controller.Initialize();
            }
        }

        private void CreateRouteController()
        {
            var spatialLayout = MapSnapshot.SpatialLayout;
            var checkpoints = new DungeonRunRoutePoint[
                spatialLayout.RouteCheckpoints.Count];
            for (var index = 0; index < checkpoints.Length; index++)
            {
                var pose = spatialLayout.RouteCheckpoints[index];
                checkpoints[index] = new DungeonRunRoutePoint(
                    pose.PositionX,
                    pose.PositionZ);
            }

            var routeProgress = new DungeonRunRouteProgress(
                checkpoints,
                spatialLayout.Encounter.StartCheckpointIndex,
                _bindings.RouteCheckpointRadius);
            _routeController = new DungeonRunRouteController(
                routeProgress,
                () => Leader.Position,
                _tickHandler);
            _routeController.PhaseChanged += OnRoutePhaseChanged;
            _routeController.Initialize();
        }

        private void OnRoutePhaseChanged(DungeonRunRoutePhase phase)
        {
            switch (phase)
            {
                case DungeonRunRoutePhase.Encounter:
                    EnterEncounter();

                    break;
                case DungeonRunRoutePhase.Continuing:
                    _teamController.ClearTacticalAnchors();
                    break;
                case DungeonRunRoutePhase.Completed:
                    if (_progress.RecordRouteCompleted())
                    {
                        ProgressChanged?.Invoke();
                    }

                    break;
            }
        }

        private void EnterEncounter()
        {
            if (_visibilityController.IsEnabled && !_visibilityController.IsZoneRevealed(1))
            {
                return;
            }

            _teamController.SetTacticalAnchors(CreateTacticalAnchors());
            ActivateEncounter();
            if (ContentPlan.EnemySpawns.Count == 0)
            {
                _routeController.CompleteEncounter();
            }
            else
            {
                ScheduleEnemyAiStart();
            }
        }

        private void ScheduleEnemyAiStart()
        {
            if (_enemyAiStartScheduled)
            {
                return;
            }

            _enemyAiStartScheduled = true;
            _tickHandler.SubscribeOnLateUpdateOnce(OnEncounterLateUpdate);
        }

        private void OnEncounterLateUpdate(float deltaTime)
        {
            _enemyAiStartScheduled = false;
            if (_isDisposed)
            {
                return;
            }

            CreateEnemyAiControllers();
        }

        private Vector3[] CreateTacticalAnchors()
        {
            var source = MapSnapshot.SpatialLayout.TacticalAnchors;
            if (source.Count < _companions.Count)
            {
                throw new InvalidOperationException(
                    "Authored dungeon requires one tactical anchor per companion.");
            }

            var result = new Vector3[_companions.Count];
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = DungeonPoseConversion.ToPosition(source[index]);
            }

            return result;
        }

        private void CreateChests()
        {
            if (_chestsCreated)
            {
                return;
            }

            _chestsCreated = true;
            for (var index = 0; index < ContentPlan.InterestPointSpawns.Count; index++)
            {
                var spawn = ContentPlan.InterestPointSpawns[index];
                if (!_chestViewLoader.Supports(spawn.InterestPointId))
                {
                    continue;
                }

                var chest = _chestFactory.Create(
                    _chestViews.Require(spawn.InterestPointId),
                    new ChestSpawnRequest(
                        $"Chest_{spawn.PlacementId}",
                        spawn.RewardProfileId,
                        DungeonPoseConversion.ToPosition(spawn.Pose),
                        DungeonPoseConversion.ToRotation(spawn.Pose)),
                    _interestsRoot.transform);
                _chests.Add(chest);
                _chestRewardPlans.Add(spawn.Rewards);
            }
        }

        private void CreateContextActions()
        {
            _contextActionsView = UnityEngine.Object.Instantiate(
                _bindings.ContextActionsPrefab,
                _combatHudView.ContextActionsHost,
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
                Leader,
                _progress,
                _rewardPickups,
                _chests,
                _tickHandler,
                model,
                _bindings.RewardPickupDistance,
                _bindings.ChestOpenDistance,
                _exitPosition,
                _bindings.ExitDistance,
                OnRewardCollected,
                OnChestOpened,
                OnExitRequested,
                _visibilityController,
                TryOpenDoor);
            _contextActionsController.Initialize();
        }

        private bool TryOpenDoor(int doorIndex)
        {
            if (!_visibilityController.TryOpenDoor(doorIndex))
            {
                return false;
            }

            CreateChests();
            if (_routeController?.Phase == DungeonRunRoutePhase.Encounter)
            {
                EnterEncounter();
            }

            return true;
        }

        private void CreateCombatHud()
        {
            _combatHudView = UnityEngine.Object.Instantiate(
                _bindings.CombatHudPrefab,
                _contextActionsParent,
                worldPositionStays: false);
            _combatHudView.name = "CombatHud";
            _combatHudView.gameObject.SetActive(true);

            var model = new CombatHudModel(
                DungeonRunCombatHudController.CreateInitialStates(
                    _heroController,
                    _leaderCombatController,
                    _skillViews));
            _combatHudViewModel = new CombatHudViewModel(
                model,
                movement => _virtualHeroInput?.SetMovement(movement),
                slot => _virtualHeroInput?.RequestSkill(slot));
            _combatHudViewModel.Initialize();
            _combatHudView.Initialize(
                _combatHudViewModel,
                disposeWithViewModel: false);

            _combatHudController = new DungeonRunCombatHudController(
                _heroController,
                _leaderCombatController,
                _worldCamera,
                _tickHandler,
                model);
            _combatHudController.Initialize();
        }

        private void SubscribeToActorDeaths()
        {
            Leader.Died += OnLeaderDied;
        }

        private void UnsubscribeFromActorDeaths()
        {
            if (Leader != null)
            {
                Leader.Died -= OnLeaderDied;
            }

            for (var index = 0; index < _enemies.Count; index++)
            {
                _enemies[index].Died -= OnEnemyDied;
            }
        }

        private void OnEnemyDied(ActorInstance enemy)
        {
            if (!_progress.RecordEnemyKilled())
            {
                return;
            }

            if (!_enemySpawnPlans.TryGetValue(enemy, out var enemySpawn))
            {
                throw new InvalidOperationException("Dead enemy does not belong to this run.");
            }

            SpawnRewardPickups(enemy.Position, enemySpawn.Rewards);
            if (_progress.RemainingEnemies == 0)
            {
                _routeController?.CompleteEncounter();
            }

            ProgressChanged?.Invoke();
        }

        private void OnLeaderDied(ActorInstance leader)
        {
            TryFinish(DungeonRunOutcome.Defeated);
        }

        private void OnChestOpened(ChestInstance chest)
        {
            var chestIndex = _chests.IndexOf(chest);
            if (chestIndex < 0)
            {
                throw new InvalidOperationException("Opened chest does not belong to this run.");
            }

            SpawnRewardPickups(chest.RewardPosition, _chestRewardPlans[chestIndex]);
        }

        private void SpawnRewardPickups(
            Vector3 position,
            IReadOnlyList<DungeonRewardGrantPlan> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                var reward = rewards[index];
                var definition = _rewardCatalog.Require(reward.RewardId);
                var offset = Vector3.right * (index - (rewards.Count - 1) * 0.5f) * 0.75f;
                var pickup = _rewardPickupFactory.Create(
                    _rewardPickupViews.Require(reward.RewardId),
                    new RewardPickupSpawnRequest(
                        position + offset,
                        definition,
                        reward.Amount),
                    _rewardsRoot.transform);
                _rewardPickups.Add(pickup);
            }
        }

        private void OnRewardCollected(RewardGrant reward)
        {
            if (_progress.CollectReward(reward))
            {
                ProgressChanged?.Invoke();
            }
        }

        private void OnExitRequested()
        {
            if (!_progress.CanExit)
            {
                return;
            }

            for (var index = 0; index < ContentPlan.CompletionRewards.Count; index++)
            {
                var reward = ContentPlan.CompletionRewards[index];
                _progress.CollectReward(new RewardGrant(reward.RewardId, reward.Amount));
            }

            TryFinish(DungeonRunOutcome.Completed);
        }

        private bool TryFinish(DungeonRunOutcome outcome)
        {
            if (!_progress.TryFinish(outcome))
            {
                return false;
            }

            _contextActionsController?.SetRunFinished();
            DisposeActiveControllers();
            DisposeSkillExecution();
            DisposeCombatControllers();
            var result = new DungeonRunResult(
                _runId,
                outcome,
                MapSnapshot.DungeonId,
                MapSnapshot.Seed,
                _progress.KilledEnemies,
                _progress.CreateCollectedRewardsSnapshot());
            Finished?.Invoke(result);
            return true;
        }

        private void DisposeActiveControllers()
        {
            if (_routeController != null)
            {
                _routeController.PhaseChanged -= OnRoutePhaseChanged;
                _routeController.Dispose();
                _routeController = null;
            }

            _wallOcclusionController?.Dispose();
            _wallOcclusionController = null;

            _cameraPresenter?.Dispose();
            _cameraPresenter = null;

            _combatHudController?.Dispose();
            _combatHudController = null;

            _virtualHeroInput?.Disable();

            _contextActionsController?.Dispose();
            _contextActionsController = null;

            for (var index = _enemyAiControllers.Count - 1; index >= 0; index--)
            {
                _enemyAiControllers[index].Dispose();
            }

            _enemyAiControllers.Clear();

            _teamController?.Dispose();
            _teamController = null;

            _heroController?.Dispose();
            _heroController = null;

            _heroInput = null;

            _input?.Dispose();
            _input = null;
            _virtualHeroInput = null;
        }

        private void DisposeCombatHudPresentation()
        {
            _combatHudView?.Dispose();
            _combatHudView = null;

            _combatHudViewModel?.Dispose();
            _combatHudViewModel = null;
        }

        private void DisposeContextActionsPresentation()
        {
            _contextActionsView?.Dispose();
            _contextActionsView = null;

            _contextActionsViewModel?.Dispose();
            _contextActionsViewModel = null;
        }

        private void DisposeSkillExecution()
        {
            _skillExecution?.Dispose();
            _skillExecution = null;
            if (_skillProjectilesRoot == null)
                return;

            DestroyGameObject(_skillProjectilesRoot);
            _skillProjectilesRoot = null;
        }

        private void DisposeCombatControllers()
        {
            for (var index = _enemyCombatControllers.Count - 1; index >= 0; index--)
            {
                _enemyCombatControllers[index].Dispose();
            }

            _enemyCombatControllers.Clear();
            for (var index = _companionCombatControllers.Count - 1; index >= 0; index--)
            {
                _companionCombatControllers[index].Dispose();
            }

            _companionCombatControllers.Clear();
            _leaderCombatController?.Dispose();
            _leaderCombatController = null;
        }

        private void ValidateRewardPlans()
        {
            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                ValidateRewards(ContentPlan.EnemySpawns[index].Rewards);
            }

            for (var index = 0; index < ContentPlan.InterestPointSpawns.Count; index++)
            {
                ValidateRewards(ContentPlan.InterestPointSpawns[index].Rewards);
            }

            ValidateRewards(ContentPlan.CompletionRewards);
        }

        private void ValidateEnemyBehaviors()
        {
            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                _enemyBehaviorCatalog.Require(ContentPlan.EnemySpawns[index].BehaviorId);
            }
        }

        private void ValidateLoadouts()
        {
            var loadoutIds = GetRequiredLoadoutIds();
            for (var index = 0; index < loadoutIds.Count; index++)
            {
                _skillCatalog.RequireLoadout(loadoutIds[index]);
            }
        }

        private void ValidateRewards(IReadOnlyList<DungeonRewardGrantPlan> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                _rewardCatalog.Require(rewards[index].RewardId);
            }
        }

        private static void DestroyGameObject(GameObject target)
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

        private IDungeonInstance RequireDungeon()
        {
            return _dungeonInstance ?? throw new InvalidOperationException(
                "Dungeon Run is not initialized.");
        }
    }
}

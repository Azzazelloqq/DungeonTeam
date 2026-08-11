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
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using LightDI.Runtime;
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
        private readonly CombatCatalog _combatCatalog;
        private readonly IRewardPickupViewLoader _rewardPickupViewLoader;
        private readonly IChestViewLoader _chestViewLoader;
        private readonly RectTransform _contextActionsParent;
        private readonly Camera _worldCamera;
        private readonly ITickHandler _tickHandler;
        private readonly RewardCatalog _rewardCatalog;
        private readonly TeamControlSettings _teamControlSettings;
        private readonly HeroControlSettings _heroControlSettings;
        private readonly EnemyBehaviorCatalog _enemyBehaviorCatalog;
        private readonly RewardPickupFactory _rewardPickupFactory = new();
        private readonly ChestFactory _chestFactory = new();
        private readonly List<ActorInstance> _heroes = new();
        private readonly List<ActorInstance> _companions = new();
        private readonly List<Vector3> _companionFormationOffsets = new();
        private readonly List<ActorInstance> _enemies = new();
        private readonly List<ActorCombatController> _companionCombatControllers = new();
        private readonly List<ActorCombatController> _enemyCombatControllers = new();
        private readonly List<EnemyAiController> _enemyAiControllers = new();
        private readonly List<RewardPickupInstance> _rewardPickups = new();
        private readonly List<ChestInstance> _chests = new();
        private readonly List<IReadOnlyList<DungeonRewardGrantPlan>> _chestRewardPlans = new();

        private IDungeonRunInput _input;
        private ActorDefinitionSet _actorDefinitions;
        private RewardPickupViewSet _rewardPickupViews;
        private ChestViewSet _chestViews;
        private HeroController _heroController;
        private ActorCombatController _leaderCombatController;
        private TeamController _teamController;
        private WallOcclusionController _wallOcclusionController;
        private DungeonRunContextActionsController _contextActionsController;
        private ContextActionsViewModel _contextActionsViewModel;
        private ContextActionsViewBase _contextActionsView;
        private DungeonRunProgress _progress;
        private IDungeonInstance _dungeonInstance;
        private DungeonRunNavigation _navigation;
        private Vector3 _exitPosition;
        private GameObject _actorsRoot;
        private GameObject _rewardsRoot;
        private GameObject _interestsRoot;
        private bool _terminalShutdownScheduled;
        private bool _isDisposed;

        public event Action ProgressChanged;

        public event Action<DungeonRunResult> Finished;

        public DungeonRunRoot(
            [Inject] IDungeonFactory dungeonFactory,
            DungeonRunStartRequest startRequest,
            DungeonRunBindings bindings,
            [Inject] IActorDefinitionLoader actorDefinitionLoader,
            [Inject] ActorConfigCatalog actorCatalog,
            [Inject] CombatCatalog combatCatalog,
            [Inject] IRewardPickupViewLoader rewardPickupViewLoader,
            [Inject] IChestViewLoader chestViewLoader,
            RectTransform contextActionsParent,
            Camera worldCamera,
            [Inject] ITickHandler tickHandler,
            IDungeonRunInput input,
            [Inject] RewardCatalog rewardCatalog,
            [Inject] TeamControlSettings teamControlSettings,
            [Inject] HeroControlSettings heroControlSettings,
            [Inject] EnemyBehaviorCatalog enemyBehaviorCatalog)
        {
            _dungeonFactory = dungeonFactory ?? throw new ArgumentNullException(nameof(dungeonFactory));
            _startRequest = startRequest ?? throw new ArgumentNullException(nameof(startRequest));
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _actorDefinitionLoader = actorDefinitionLoader ??
                throw new ArgumentNullException(nameof(actorDefinitionLoader));
            _actorCatalog = actorCatalog ?? throw new ArgumentNullException(nameof(actorCatalog));
            _combatCatalog = combatCatalog ?? throw new ArgumentNullException(nameof(combatCatalog));
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

        public int EnemyCount => _enemies.Count;

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
            ValidateRewardPlans();
            ValidateEnemyBehaviors();

            _navigation = new DungeonRunNavigation();
            _navigation.Build();
            ownerToken.ThrowIfCancellationRequested();

            _actorDefinitions = await _actorDefinitionLoader.LoadAsync(
                GetRequiredActorIds(),
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
            CreateActors();
            _progress = new DungeonRunProgress(_enemies.Count);
            _exitPosition = _navigation.RequireSpawnPosition(
                DungeonPoseConversion.ToPosition(MapSnapshot.ExitPose));
            _rewardsRoot = new GameObject("DungeonRunRewards");
            _interestsRoot = new GameObject("DungeonRunInterests");
            CreateChests();
            SubscribeToActorDeaths();

            _input.Enable();

            _heroController = new HeroController(
                Leader,
                _enemies,
                _worldCamera,
                _tickHandler,
                _input,
                _heroControlSettings,
                _leaderCombatController);
            _heroController.Initialize();

            _teamController = new TeamController(
                Leader,
                _companions,
                _companionCombatControllers,
                _companionFormationOffsets,
                _enemies,
                _worldCamera,
                _tickHandler,
                _input,
                _teamControlSettings);
            _teamController.Initialize();

            _wallOcclusionController = new WallOcclusionController(
                _worldCamera,
                _heroes,
                _tickHandler,
                _bindings);
            _wallOcclusionController.Initialize();

            CreateEnemyAiControllers();
            CreateContextActions();
        }

        protected override void OnDispose()
        {
            _isDisposed = true;
            DisposeActiveControllers();

            _contextActionsView?.Dispose();
            _contextActionsView = null;

            _contextActionsViewModel?.Dispose();
            _contextActionsViewModel = null;
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
            _enemyCombatControllers.Clear();

            for (var index = _heroes.Count - 1; index >= 0; index--)
            {
                _heroes[index].Dispose();
            }

            Leader = null;
            _companions.Clear();
            _companionCombatControllers.Clear();
            _leaderCombatController = null;
            _companionFormationOffsets.Clear();
            _heroes.Clear();

            if (_actorsRoot != null)
            {
                DestroyGameObject(_actorsRoot);
                _actorsRoot = null;
            }

            _actorDefinitions?.Dispose();
            _actorDefinitions = null;

            _rewardPickupViews?.Dispose();
            _rewardPickupViews = null;

            _chestViews?.Dispose();
            _chestViews = null;

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
                _startRequest.Team.Leader);
            _leaderCombatController = CreateCombatController(
                Leader,
                _startRequest.Team.Leader);
            _heroes.Add(Leader);

            for (var index = 0;
                 index < _startRequest.Team.Companions.Count;
                 index++)
            {
                var formationOffset = _bindings.GetCompanionSpawnOffset(index);
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

            for (var index = 0; index < ContentPlan.EnemySpawns.Count; index++)
            {
                var enemySpawn = ContentPlan.EnemySpawns[index];
                var enemy = CreateActor(
                    actorFactory,
                    $"Enemy_{enemySpawn.PlacementId}",
                    _navigation.RequireSpawnPosition(
                        DungeonPoseConversion.ToPosition(enemySpawn.Pose)),
                    DungeonPoseConversion.ToRotation(enemySpawn.Pose),
                    new DungeonRunActorSelection(
                        enemySpawn.EnemyId,
                        enemySpawn.ActorLevel));
                _enemies.Add(enemy);
                _enemyCombatControllers.Add(CreateCombatController(
                    enemy,
                    new DungeonRunActorSelection(
                        enemySpawn.EnemyId,
                        enemySpawn.ActorLevel)));
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
            var runtimeDefinition = _actorCatalog.Resolve(
                selection.ActorId,
                selection.Level);
            var attack = _combatCatalog.ResolvePrimaryAttack(
                runtimeDefinition.CombatLoadoutId,
                runtimeDefinition.PrimaryAttackRank);
            return new ActorCombatController(actor, attack);
        }

        private void CreateEnemyAiControllers()
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                var behaviorId = ContentPlan.EnemySpawns[index].BehaviorId;
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

        private void CreateChests()
        {
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
                _heroController,
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
                OnExitRequested);
            _contextActionsController.Initialize();
        }

        private void SubscribeToActorDeaths()
        {
            Leader.Died += OnLeaderDied;
            for (var index = 0; index < _enemies.Count; index++)
            {
                _enemies[index].Died += OnEnemyDied;
            }
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

            var enemyIndex = _enemies.IndexOf(enemy);
            if (enemyIndex < 0)
            {
                throw new InvalidOperationException("Dead enemy does not belong to this run.");
            }

            SpawnRewardPickups(enemy.Position, ContentPlan.EnemySpawns[enemyIndex].Rewards);
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
            var result = new DungeonRunResult(
                outcome,
                MapSnapshot.DungeonId,
                MapSnapshot.Seed,
                _progress.KilledEnemies,
                _progress.CreateCollectedRewardsSnapshot());
            Finished?.Invoke(result);
            ScheduleTerminalShutdown();
            return true;
        }

        private void ScheduleTerminalShutdown()
        {
            if (_terminalShutdownScheduled)
            {
                return;
            }

            _terminalShutdownScheduled = true;
            _tickHandler.SubscribeOnLateUpdateOnce(OnTerminalLateUpdate);
        }

        private void OnTerminalLateUpdate(float deltaTime)
        {
            _terminalShutdownScheduled = false;
            if (_isDisposed)
            {
                return;
            }

            DisposeActiveControllers();
        }

        private void DisposeActiveControllers()
        {
            _wallOcclusionController?.Dispose();
            _wallOcclusionController = null;

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

            _input?.Dispose();
            _input = null;
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

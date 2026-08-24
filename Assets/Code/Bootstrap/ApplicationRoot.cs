using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Azzazelloqq.Config;
using Code.Addressables.Generated;
using Code.Configuration;
using Code.UI.LoadingScreen;
using Code.UIService;
using Cysharp.Threading.Tasks;
using DungeonTeam.Feedback.Runtime;
using DungeonTeam.Feedback.Runtime.Audio;
using DungeonTeam.Feedback.Runtime.Banks;
using DungeonTeam.Feedback.Runtime.Haptics;
using DungeonTeam.Feedback.Runtime.Music;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Chests.Runtime;
using DungeonTeam.Gameplay.Contracts.Application;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.Contracts.Infrastructure;
using DungeonTeam.Gameplay.Quests.Application;
using DungeonTeam.Gameplay.Quests.Domain;
using DungeonTeam.Gameplay.Quests.Infrastructure;
using DungeonTeam.Gameplay.Quests.Runtime;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Runtime.Config;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Config;
using DungeonTeam.Gameplay.GuildHall.Runtime.Composition;
using DungeonTeam.Gameplay.GuildHall.Runtime.Input;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Infrastructure;
using DungeonTeam.UI.WorldMap;
using DungeonTeam.DeveloperTools;
using LightDI.Runtime;
using LocalSaveSystem;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
using RootPattern;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code.ApplicationRoot
{
	internal sealed class ApplicationRoot : Root
	{
		private readonly UICanvasContext _canvasContext;
		private readonly ConfigCatalog _configCatalog;
		private readonly Camera _worldCamera;
		private readonly DungeonRunBindings _dungeonRunBindings;
		private readonly TeamControlSettings _teamControlSettings;
		private readonly HeroControlSettings _heroControlSettings;
		private readonly FeedbackRuntimeSettings _feedbackRuntimeSettings;

		private IDiContainer _globalContainer;
		private IUiService _uiService;
		private LoadingScreenViewBase _loadingScreen;
		private LoadingScreenViewModel _loadingScreenViewModel;
		private IResourceLoader _resourceLoader;
		private IDungeonFactory _dungeonFactory;
		private IActorDefinitionLoader _actorDefinitionLoader;
		private ActorConfigCatalog _actorCatalog;
		private SkillCatalog _skillCatalog;
		private ISkillViewLoader _skillViewLoader;
		private IRewardPickupViewLoader _rewardPickupViewLoader;
		private IChestViewLoader _chestViewLoader;
		private RewardCatalog _rewardCatalog;
		private EnemyBehaviorCatalog _enemyBehaviorCatalog;
		private DungeonRunTeamSetup _dungeonRunTeamSetup;
		private PlayerProfilePersistence _playerProfilePersistence;
		private PlayerProfileSession _playerProfileSession;
		private ItemCatalog _itemCatalog;
		private GuildProfileEditHandler _guildProfileEditHandler;
		private DungeonRunLaunchPresetCatalog _launchPresetCatalog;
		private GuildHallCatalog _guildHallCatalog;
		private DialogueCatalog _dialogueCatalog;
		private AmbientNpcProfileCatalog _ambientNpcProfileCatalog;
		private DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog _contractCatalog;
		private ContractPersistence _contractPersistence;
		private ContractSession _contractSession;
		private QuestConfigPage _questConfigPage;
		private QuestCatalog _questCatalog;
		private QuestPersistence _questPersistence;
		private QuestSession _questSession;
		private GuildRankCatalog _guildRankCatalog;
		private WorldMapCatalog _worldMapCatalog;
		private GuildSessionState _guildSessionState;
		private GuildHallRoot _guildHallRoot;
		private WorldMapRoot _worldMapRoot;
		private ApplicationTransitionGate _transitionGate;
		private ITickHandler _tickHandler;
		private IFeedbackService _feedbackService;
		private IMusicPlayer _musicPlayer;
		private FeedbackBankLoader _feedbackBankLoader;
		private DungeonRunHost _dungeonRunHost;
		private DungeonRunRoot _finishedRunSubscription;
		private string _activeRunContractId;
		private GuildRunSummaryBuilder _runSummaryBuilder;
		private RewardSettlementMapper _rewardSettlementMapper;
		private DeveloperRunConsoleController _developerConsoleController;
		private DeveloperRunConsoleView _developerConsoleView;

		public ApplicationRoot(
			UICanvasContext canvasContext,
			ConfigCatalog configCatalog,
			Camera worldCamera,
			DungeonRunBindings dungeonRunBindings,
			TeamControlSettings teamControlSettings,
			HeroControlSettings heroControlSettings,
			FeedbackRuntimeSettings feedbackRuntimeSettings)
		{
			_canvasContext = canvasContext;
			_configCatalog = configCatalog;
			_worldCamera = worldCamera != null
				? worldCamera
				: throw new ArgumentNullException(nameof(worldCamera));
			_dungeonRunBindings = dungeonRunBindings ??
				throw new ArgumentNullException(nameof(dungeonRunBindings));
			_teamControlSettings = teamControlSettings ??
				throw new ArgumentNullException(nameof(teamControlSettings));
			_heroControlSettings = heroControlSettings ??
				throw new ArgumentNullException(nameof(heroControlSettings));
			_feedbackRuntimeSettings = feedbackRuntimeSettings ??
				throw new ArgumentNullException(nameof(feedbackRuntimeSettings));
		}

		protected override async UniTask OnInitializeAsync(CancellationToken token)
		{
			_globalContainer = DiContainerFactory.CreateGlobalContainer();

			_resourceLoader = new AddressableResourceLoader();
			_uiService = new UIService.UIService(_resourceLoader, _canvasContext);
			_globalContainer.RegisterAsSingleton(_uiService);
			_globalContainer.RegisterAsSingleton(_resourceLoader);

			await ShowLoadingScreenAsync(token);

			IConfig config = new Config(new ScriptableObjectConfigParser(_configCatalog));
			_globalContainer.RegisterAsSingleton(config);
			await config.InitializeAsync(token);
			_dungeonFactory = new DungeonFactory(config.GetConfigPage<DungeonConfigPage>());
			_actorCatalog = config.GetConfigPage<ActorConfigPage>().CreateCatalog();
			_skillCatalog = config.GetConfigPage<SkillConfigPage>().CreateCatalog();
			_skillViewLoader = new SkillViewLoader(_skillCatalog, _resourceLoader);
			_actorDefinitionLoader = new ActorDefinitionLoader(_actorCatalog, _resourceLoader);
			_dungeonRunTeamSetup = config
				.GetConfigPage<DungeonRunConfigPage>()
				.CreateTeamSetup(_actorCatalog, _skillCatalog);
			_itemCatalog = config
				.GetConfigPage<ItemConfigPage>()
				.CreateCatalog(GetRosterActorIds(_dungeonRunTeamSetup));
			_playerProfileSession = PlayerProfileComposition.Create(
				_dungeonRunTeamSetup,
				_itemCatalog,
				out _playerProfilePersistence);
			_contractPersistence = new ContractPersistence(new SaveStoreOptions(
				Path.Combine(Application.persistentDataPath, "DungeonTeam"))
			{
				UseTaggedFormat = true,
				UseAtomicWrite = true,
				SaveOnQuit = true
			});
			_contractSession = new ContractSession(_contractPersistence.Repository);
			_guildRankCatalog = config
				.GetConfigPage<GuildRankConfigPage>()
				.CreateCatalog();
			_launchPresetCatalog = config
				.GetConfigPage<DungeonRunLaunchConfigPage>()
				.CreateCatalog();
			_guildHallCatalog = config.GetConfigPage<GuildHallConfigPage>().CreateCatalog();
			_guildProfileEditHandler = new GuildProfileEditHandler(
				_playerProfileSession,
				_dungeonRunTeamSetup,
				_guildHallCatalog.ProfileText,
				BuildGuildProfileSnapshot,
				_itemCatalog,
				_guildRankCatalog,
				Debug.LogException);
			_dialogueCatalog = config.GetConfigPage<DialogueConfigPage>().CreateCatalog();
			_ambientNpcProfileCatalog = config.GetConfigPage<AmbientNpcConfigPage>().CreateCatalog();
			var contractConfig = config.GetConfigPage<ContractConfigPage>();
			_contractCatalog = contractConfig.CreateCatalog();
			_worldMapCatalog = config.GetConfigPage<WorldMapConfigPage>().CreateCatalog();
			_questConfigPage = config.GetConfigPage<QuestConfigPage>();
			_questCatalog = _questConfigPage.CreateCatalog();
			_questPersistence = new QuestPersistence(new SaveStoreOptions(
				Path.Combine(Application.persistentDataPath, "DungeonTeam"))
			{
				UseTaggedFormat = true,
				UseAtomicWrite = true,
				SaveOnQuit = true
			});
			_questSession = new QuestSession(_questPersistence.Repository);
			_questSession.State.ValidateAgainst(_questCatalog);
			ValidateQuestTargets(_questCatalog, _launchPresetCatalog, _itemCatalog, _guildHallCatalog);
			GuildContentValidator.Validate(
				_guildHallCatalog,
				_dialogueCatalog,
				_ambientNpcProfileCatalog,
				_contractCatalog,
				_worldMapCatalog.ContractDestinationLocationIds);
			_contractCatalog.ValidateSupportedLocations(_worldMapCatalog.ContractDestinationLocationIds);
			for (var index = 0; index < _worldMapCatalog.Locations.Count; index++)
			{
				var location = _worldMapCatalog.Locations[index];
				if (location.DestinationKind == WorldLocationDestinationKind.DungeonRun)
				{
					_launchPresetCatalog.Require(location.DestinationId);
				}
			}
			_rewardPickupViewLoader = new RewardPickupViewLoader(_resourceLoader);
			_chestViewLoader = new ChestViewLoader(_resourceLoader);
			_rewardCatalog = config.GetConfigPage<RewardConfigPage>().CreateCatalog();
			_enemyBehaviorCatalog = config
				.GetConfigPage<EnemyBehaviorConfigPage>()
				.CreateCatalog();

			var dispatcher = new GameObject("TickHandlerDispatcher");
			var unityDispatcherBehaviour = dispatcher.AddComponent<UnityDispatcherBehaviour>();
			_tickHandler = new UnityTickHandler(unityDispatcherBehaviour);
			_globalContainer.RegisterAsSingleton(_tickHandler);

			_feedbackRuntimeSettings.Validate();
			_feedbackService = CreateFeedbackService(_tickHandler, _feedbackRuntimeSettings);
			_globalContainer.RegisterAsSingleton(_feedbackService);
			_musicPlayer = new MusicPlayer();
			_globalContainer.RegisterAsSingleton(_musicPlayer);
			_feedbackBankLoader = new FeedbackBankLoader(_resourceLoader, _feedbackService);
			_globalContainer.RegisterAsSingleton(_feedbackBankLoader);
			_dungeonRunHost = new DungeonRunHost(CreateDungeonRunRoot);
			_runSummaryBuilder = new GuildRunSummaryBuilder();
			_rewardSettlementMapper = new RewardSettlementMapper();

			_guildSessionState = new GuildSessionState();
			_transitionGate = new ApplicationTransitionGate(PlayerFlowState.Initializing);
			await CreateGuildHallAsync(token);
			if (!_transitionGate.TryBegin(PlayerFlowState.Initializing, out var startupLease))
			{
				throw new InvalidOperationException("Application startup transition was rejected.");
			}

			startupLease.Complete(PlayerFlowState.GuildHall);

			if (DeveloperRunConsoleAvailability.IsEnabled(
				    Application.isEditor,
				    Debug.isDebugBuild))
			{
				CreateDeveloperConsole();
			}

			await HideLoadingScreenAsync(token);
		}

		protected override void OnDispose()
		{
			_transitionGate?.Dispose();
			_worldMapRoot?.Dispose();
			_worldMapRoot = null;
			_guildHallRoot?.Dispose();
			_guildHallRoot = null;
			DisposeDungeonRun();
			if (_developerConsoleView != null)
			{
				UnityEngine.Object.Destroy(_developerConsoleView.gameObject);
				_developerConsoleView = null;
			}

			_developerConsoleController = null;
			_dungeonRunHost = null;
			_activeRunContractId = null;
			_runSummaryBuilder = null;
			_rewardSettlementMapper = null;
			_dungeonFactory = null;
			_actorDefinitionLoader = null;
			_actorCatalog = null;
			_skillCatalog = null;
			_skillViewLoader = null;
			_rewardPickupViewLoader = null;
			_chestViewLoader = null;
			_rewardCatalog = null;
			_enemyBehaviorCatalog = null;
			_dungeonRunTeamSetup = null;
			_playerProfileSession = null;
			_contractSession = null;
			_questSession = null;
			_itemCatalog = null;
			_guildRankCatalog = null;
			_guildProfileEditHandler = null;
			_playerProfilePersistence?.Dispose();
			_playerProfilePersistence = null;
			_contractPersistence?.Dispose();
			_contractPersistence = null;
			_questPersistence?.Dispose();
			_questPersistence = null;
			_questConfigPage = null;
			_questCatalog = null;
			_launchPresetCatalog = null;
			_guildHallCatalog = null;
			_dialogueCatalog = null;
			_ambientNpcProfileCatalog = null;
			_contractCatalog = null;
			_worldMapCatalog = null;
			_guildSessionState = null;
			_transitionGate = null;
			_resourceLoader = null;

			_loadingScreenViewModel?.Dispose();
			_loadingScreenViewModel = null;
			_loadingScreen = null;

			try
			{
				_globalContainer?.Dispose();
			}
			finally
			{
				_globalContainer = null;
				_feedbackBankLoader = null;
				_musicPlayer = null;
				_feedbackService = null;
				_tickHandler = null;
				_uiService = null;
			}
		}

		public void SetApplicationPaused(bool isPaused)
		{
			if (isPaused)
			{
				_feedbackService?.StopAll();
			}
		}

		private async UniTask ShowLoadingScreenAsync(CancellationToken token)
		{
			if (_loadingScreen == null)
			{
				_loadingScreen = await _uiService.CreateAsync<LoadingScreenViewBase>(
					AddressableIds.UI.WindowsMainLoadingScreen,
					token: token);

				_loadingScreenViewModel = new LoadingScreenViewModel(new LoadingScreenModel());
				_loadingScreenViewModel.Initialize();
				_loadingScreen.Initialize(_loadingScreenViewModel, disposeWithViewModel: false);
			}

			await _uiService.ShowAsync(_loadingScreen, token);
		}

		private async UniTask HideLoadingScreenAsync(CancellationToken token)
		{
			await _uiService.HideAsync(_loadingScreen, token);
		}

		private async UniTask CreateGuildHallAsync(CancellationToken token)
		{
			var hall = new GuildHallRoot(
				new GuildHallWorldLoader(_resourceLoader),
				WithProfile(BuildGuildHallStartContext()),
				_guildHallCatalog, _ambientNpcProfileCatalog, _dialogueCatalog, _tickHandler,
#if UNITY_EDITOR
				new EditorGuildHallInput(),
#else
				new EditorGuildHallInput(),
#endif
				_ => { }, OnGuildHallWorldMapRequested, AcceptContract, AcceptQuest,
				OnDialogueCompleted, _guildProfileEditHandler.Handle, true);
			await hall.InitializeAsync(token);
			_guildHallRoot = hall;
		}

		private GuildHallStartContext BuildGuildHallStartContext()
		{
			var offers = ContractSnapshotBuilder.Build(
				_contractCatalog,
				_contractSession.State,
				_guildRankCatalog,
				_playerProfileSession.State.RankId,
				_guildHallCatalog.ProfileText.RequiredRankOfferFormat,
				_guildHallCatalog.NoticeBoardText);
			var quests = BuildQuestBoardSnapshots();
			return new GuildHallStartContext(
				_guildHallCatalog.Npcs,
				offers,
				_contractSession.State.ActiveContractId,
				_guildSessionState.LastRunSummary,
				null,
				quests);
		}

		private QuestBoardEntrySnapshot[] BuildQuestBoardSnapshots()
		{
			var snapshots = new QuestBoardEntrySnapshot[_questCatalog.Definitions.Count];
			for (var index = 0; index < snapshots.Length; index++)
			{
				var definition = _questCatalog.Definitions[index];
				var completed = _questSession.State.IsCompleted(definition.QuestId);
				var accepted = _questSession.State.IsActive(definition.QuestId);
				var unlocked = _questCatalog.IsQuestUnlocked(definition.QuestId, _questSession.State);
				var status = completed
					? QuestBoardEntrySnapshot.EntryStatus.Completed
					: accepted
						? QuestBoardEntrySnapshot.EntryStatus.Accepted
						: unlocked
							? QuestBoardEntrySnapshot.EntryStatus.Available
							: QuestBoardEntrySnapshot.EntryStatus.Locked;
				var current = completed ? definition.Objective.RequiredProgress : _questSession.State.GetProgress(definition.QuestId);
				var statusText = status switch
				{
					QuestBoardEntrySnapshot.EntryStatus.Completed => _guildHallCatalog.NoticeBoardText.QuestCompleted,
					QuestBoardEntrySnapshot.EntryStatus.Accepted => _guildHallCatalog.NoticeBoardText.QuestAccepted,
					QuestBoardEntrySnapshot.EntryStatus.Locked => _guildHallCatalog.NoticeBoardText.QuestLocked,
					_ => _guildHallCatalog.NoticeBoardText.QuestAccept
				};
				snapshots[index] = new QuestBoardEntrySnapshot(
					definition.QuestId,
					new GuildTextSnapshot(definition.Title.TextId, definition.Title.DisplayText),
					new GuildTextSnapshot(definition.Summary.TextId, definition.Summary.DisplayText),
					new GuildTextSnapshot(definition.ObjectiveText.TextId, definition.ObjectiveText.DisplayText),
					new GuildTextSnapshot($"quest.{definition.QuestId}.progress", $"{current}/{definition.Objective.RequiredProgress}"),
					new GuildTextSnapshot(statusText.TextId, statusText.DisplayText),
					status == QuestBoardEntrySnapshot.EntryStatus.Available,
					status);
			}
			return snapshots;
		}

		private bool AcceptQuest(string questId)
		{
			if (!_questCatalog.Contains(questId) || !_questCatalog.IsQuestUnlocked(questId, _questSession.State) ||
				_questSession.State.IsActive(questId) || _questSession.State.IsCompleted(questId)) return false;
			return _questSession.Accept(questId, _questCatalog);
		}

		private void OnDialogueCompleted(string npcId)
		{
			_questSession?.RecordDialogueCompleted(npcId, _questCatalog);
		}

		private bool AcceptContract(string contractId)
		{
			if (!ContractSnapshotBuilder.IsAvailableForAcceptance(
					contractId,
					_contractCatalog,
					_contractSession.State,
					_guildRankCatalog,
					_playerProfileSession.State.RankId,
					_guildHallCatalog.ProfileText.RequiredRankOfferFormat,
					_guildHallCatalog.NoticeBoardText))
			{
				return false;
			}

			var result = _contractSession.Accept(contractId, _contractCatalog);
			if (!result.Accepted)
			{
				return false;
			}

			_guildSessionState.SelectContract(contractId);
			return true;
		}

		private GuildHallStartContext WithProfile(GuildHallStartContext context) => new(
			context.Npcs, context.Offers, context.SelectedContractId, context.LastRunSummary,
			BuildGuildProfileSnapshot(_playerProfileSession.State), context.QuestEntries);

		private GuildProfileSnapshot BuildGuildProfileSnapshot(
			PlayerProfileState profile) =>
			GuildProfileSnapshotBuilder.Build(
				profile,
				_actorCatalog,
				_skillCatalog,
				_dungeonRunTeamSetup,
				_guildHallCatalog.ProfileText,
				_itemCatalog,
				_guildRankCatalog);

		private void OnGuildHallWorldMapRequested() =>
			TransitionToWorldMapAsync(CancellationToken).Forget(Debug.LogException);

		private void OnWorldMapBackRequested() =>
			TransitionToGuildHallAsync(CancellationToken).Forget(Debug.LogException);

		private void OnWorldMapLocationSelected(string locationId) =>
			TransitionFromWorldMapAsync(locationId, CancellationToken).Forget(Debug.LogException);

		private async UniTask TransitionToWorldMapAsync(CancellationToken token)
		{
			if (!_transitionGate.TryBegin(PlayerFlowState.GuildHall, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			try
			{
				_guildHallRoot.SetWorldInputBlocked(true);
				await ShowLoadingScreenAsync(token);
				var map = await CreateWorldMapAsync(token);
				_guildHallRoot.Dispose();
				_guildHallRoot = null;
				_worldMapRoot = map;
				await map.ShowAsync(token);
				lease.Complete(PlayerFlowState.WorldMap);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				// Application shutdown owns final cleanup.
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				canHideLoading = await TryRestoreGuildHallAsync(token);
				if (!canHideLoading)
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private async UniTask TransitionToGuildHallAsync(CancellationToken token)
		{
			if (!_transitionGate.TryBegin(PlayerFlowState.WorldMap, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			try
			{
				await ShowLoadingScreenAsync(token);
				await CloseWorldMapAsync(token);
				await CreateGuildHallAsync(token);
				lease.Complete(PlayerFlowState.GuildHall);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				// Application shutdown owns final cleanup.
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				canHideLoading = await TryRestoreWorldMapAsync(token);
				if (!canHideLoading)
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private async UniTask TransitionFromWorldMapAsync(string locationId, CancellationToken token)
		{
			WorldMapDestination destination;
			try
			{
				destination = new WorldMapDestinationResolver(
					_worldMapCatalog,
					_contractCatalog,
					_contractSession.State,
					_launchPresetCatalog,
					PlayerProfileComposition.MapToTeamSelection(_playerProfileSession.State, _itemCatalog))
					.Resolve(locationId);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				_worldMapRoot?.RestoreInteraction();
				return;
			}

			if (destination.IsUnavailable)
			{
				_worldMapRoot?.RestoreInteraction();
				return;
			}

			if (destination.IsGuildHall)
			{
				await TransitionToGuildHallAsync(token);
				return;
			}

			await TransitionToDungeonAsync(destination.Request, token);
		}

		private async UniTask TransitionToDungeonAsync(
			DungeonRunStartRequest request,
			CancellationToken token)
		{
			if (!_transitionGate.TryBegin(PlayerFlowState.WorldMap, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			try
			{
				await ShowLoadingScreenAsync(token);
				await CloseWorldMapAsync(token);
				_activeRunContractId = request.ContractId;
				await _dungeonRunHost.StartAsync(request, token);
				SubscribeToDungeonRunFinished(_dungeonRunHost.ActiveRun);
				lease.Complete(PlayerFlowState.DungeonRun);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				DisposeDungeonRun();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				DisposeDungeonRun();
				canHideLoading = await TryRestoreWorldMapAsync(token);
				if (!canHideLoading)
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private async UniTask StartDeveloperRunAsync(DungeonRunStartRequest request, CancellationToken token)
		{
			var previousState = _transitionGate.State;
			if (!_transitionGate.TryBegin(previousState, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			try
			{
				_guildHallRoot?.SetWorldInputBlocked(true);
				await ShowLoadingScreenAsync(token);
				await CloseWorldMapAsync(token);
				_guildHallRoot?.Dispose();
				_guildHallRoot = null;
				DisposeDungeonRun();
				_activeRunContractId = null;
				await _dungeonRunHost.StartAsync(request, token);
				SubscribeToDungeonRunFinished(_dungeonRunHost.ActiveRun);
				lease.Complete(PlayerFlowState.DungeonRun);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				DisposeDungeonRun();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				DisposeDungeonRun();
				if (previousState == PlayerFlowState.WorldMap)
				{
					canHideLoading = await TryRestoreWorldMapAsync(token);
				}
				else
				{
					canHideLoading = await TryRestoreGuildHallAsync(token);
					if (canHideLoading &&
					    previousState is PlayerFlowState.DungeonRun or PlayerFlowState.Faulted)
					{
						lease.Complete(PlayerFlowState.GuildHall);
					}
				}
				if (!canHideLoading)
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private async UniTask ReturnFromDeveloperRunAsync(CancellationToken token)
		{
			if (!_transitionGate.TryBegin(PlayerFlowState.DungeonRun, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			try
			{
				await ShowLoadingScreenAsync(token);
				DisposeDungeonRun();
				await CreateGuildHallAsync(token);
				lease.Complete(PlayerFlowState.GuildHall);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				DisposeDungeonRun();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				canHideLoading = await TryRestoreGuildHallAsync(token);
				if (canHideLoading)
				{
					lease.Complete(PlayerFlowState.GuildHall);
				}
				else
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private async UniTask<WorldMapRoot> CreateWorldMapAsync(CancellationToken token)
		{
			var map = new WorldMapRoot(
				_uiService,
				_worldMapCatalog.CreateStartContext(),
				OnWorldMapLocationSelected,
				OnWorldMapBackRequested);
			await map.InitializeAsync(token);
			return map;
		}

		private async UniTask CloseWorldMapAsync(CancellationToken token)
		{
			var map = _worldMapRoot;
			if (map == null)
			{
				return;
			}

			await map.CloseAsync(token);
			_worldMapRoot = null;
			map.Dispose();
		}

		private async UniTask<bool> TryRestoreWorldMapAsync(CancellationToken token)
		{
			if (token.IsCancellationRequested)
			{
				return false;
			}

			try
			{
				if (_guildHallRoot != null)
				{
					_guildHallRoot.Dispose();
					_guildHallRoot = null;
				}

				if (_worldMapRoot == null)
				{
					_worldMapRoot = await CreateWorldMapAsync(token);
				}

				_worldMapRoot.RestoreInteraction();
				await _worldMapRoot.ShowAsync(token);
				return true;
			}
			catch (Exception recoveryException)
			{
				Debug.LogException(recoveryException);
				return false;
			}
		}

		private async UniTask<bool> TryRestoreGuildHallAsync(CancellationToken token)
		{
			if (token.IsCancellationRequested)
			{
				return false;
			}

			try
			{
				if (_worldMapRoot != null)
				{
					await CloseWorldMapAsync(token);
				}

				if (_guildHallRoot == null)
				{
					await CreateGuildHallAsync(token);
				}

				_guildHallRoot.SetWorldInputBlocked(false);
				return true;
			}
			catch (Exception recoveryException)
			{
				Debug.LogException(recoveryException);
				return false;
			}
		}

		private async UniTask HideLoadingSafelyAsync(CancellationToken token)
		{
			try
			{
				await HideLoadingScreenAsync(token);
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				// Application shutdown owns final cleanup.
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void DisposeDungeonRun()
		{
			UnsubscribeFromDungeonRunFinished();
			_dungeonRunHost?.Stop();
			_activeRunContractId = null;
		}

		private void SubscribeToDungeonRunFinished(DungeonRunRoot run)
		{
			if (run == null)
			{
				throw new InvalidOperationException("Dungeon Run host has no active run to subscribe.");
			}

			UnsubscribeFromDungeonRunFinished();
			_finishedRunSubscription = run;
			run.Finished += OnDungeonRunFinished;
		}

		private void UnsubscribeFromDungeonRunFinished()
		{
			if (_finishedRunSubscription == null)
			{
				return;
			}

			_finishedRunSubscription.Finished -= OnDungeonRunFinished;
			_finishedRunSubscription = null;
		}

		private void OnDungeonRunFinished(DungeonRunResult result)
		{
			ReturnFromFinishedDungeonRunAsync(result, CancellationToken)
				.Forget(Debug.LogException);
		}

		private async UniTask ReturnFromFinishedDungeonRunAsync(
			DungeonRunResult result,
			CancellationToken token)
		{
			var finishedRun = _finishedRunSubscription;
			if (finishedRun == null || !ReferenceEquals(_dungeonRunHost?.ActiveRun, finishedRun) ||
				!_transitionGate.TryBegin(PlayerFlowState.DungeonRun, out var lease))
			{
				return;
			}

			var canHideLoading = false;
			var isRunStopped = false;
			try
			{
				var request = _rewardSettlementMapper.Map(result, _rewardCatalog);
				var settlement = _playerProfileSession.BankTerminalResult(request);
				if (!settlement.IsApplied)
				{
					UnsubscribeFromDungeonRunFinished();
					_dungeonRunHost.Stop();
					isRunStopped = true;
					_guildSessionState.ClearLastRunSummary();
					await ShowLoadingScreenAsync(token);
					await CreateGuildHallAsync(token);
					lease.Complete(PlayerFlowState.GuildHall);
					canHideLoading = true;
					return;
				}

				var summary = _runSummaryBuilder.Build(
					result,
					settlement.Receipt,
					_rewardCatalog,
					_guildHallCatalog.RunSummaryText);
				if (result.Outcome == DungeonRunOutcome.Completed)
				{
					_questSession.RecordDungeonCompleted(result.DungeonId, _questCatalog);
					var grants = new QuestResourceGrant[settlement.Receipt.ResourceGrants.Count];
					for (var index = 0; index < grants.Length; index++)
					{
						var grant = settlement.Receipt.ResourceGrants[index];
						grants[index] = new QuestResourceGrant(grant.DefinitionId, grant.Amount);
					}
					_questSession.RecordSettledResources(grants, _questCatalog);
				}
				if (result.Outcome == DungeonRunOutcome.Completed &&
					!string.IsNullOrWhiteSpace(_activeRunContractId))
				{
					var completion = _contractSession.CompleteActive(_activeRunContractId);
					if (completion.Completed)
					{
						_guildSessionState.ClearSelectedContract();
					}
				}
				await ShowLoadingScreenAsync(token);
				UnsubscribeFromDungeonRunFinished();
				_dungeonRunHost.Stop();
				isRunStopped = true;
				_guildSessionState.SetLastRunSummary(summary);
				await CreateGuildHallAsync(token);
				lease.Complete(PlayerFlowState.GuildHall);
				canHideLoading = true;
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				// Application shutdown owns final cleanup.
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				if (!isRunStopped)
				{
					UnsubscribeFromDungeonRunFinished();
					_dungeonRunHost.Stop();
					isRunStopped = true;
				}

				_guildSessionState.ClearLastRunSummary();
				_guildHallRoot?.Dispose();
				_guildHallRoot = null;
				canHideLoading = await TryRestoreGuildHallAsync(token);
				if (canHideLoading)
				{
					lease.Complete(PlayerFlowState.GuildHall);
				}
				else
				{
					lease.Complete(PlayerFlowState.Faulted);
				}
			}
			finally
			{
				lease.Dispose();
				if (canHideLoading && !token.IsCancellationRequested)
				{
					await HideLoadingSafelyAsync(token);
				}
			}
		}

		private DungeonRunRoot CreateDungeonRunRoot(DungeonRunStartRequest request)
		{
			_dungeonRunTeamSetup.RequireValid(request.Team);
			return new DungeonRunRoot(
				_dungeonFactory,
				request,
				_dungeonRunBindings,
				_actorDefinitionLoader,
				_actorCatalog,
				_skillCatalog,
				_skillViewLoader,
				_rewardPickupViewLoader,
				_chestViewLoader,
				_canvasContext.GetParent(UIElementGroup.OverlayElement),
				_worldCamera,
				_tickHandler,
#if UNITY_EDITOR
				new EditorDungeonRunInput(),
#else
				new MobileDungeonRunInput(),
#endif
				_rewardCatalog,
				_teamControlSettings,
				_heroControlSettings,
				_enemyBehaviorCatalog);
		}

		private static IReadOnlyCollection<string> GetRosterActorIds(DungeonRunTeamSetup teamSetup)
		{
			var actorIds = new string[teamSetup.Members.Count];
			for (var index = 0; index < actorIds.Length; index++)
			{
				actorIds[index] = teamSetup.Members[index].ActorId;
			}

			return actorIds;
		}

		private static void ValidateQuestTargets(
			QuestCatalog quests,
			DungeonRunLaunchPresetCatalog launchPresets,
			ItemCatalog items,
			GuildHallCatalog guildHall)
		{
			if (quests == null || launchPresets == null || items == null || guildHall == null)
				throw new ArgumentNullException(nameof(quests));
			for (var index = 0; index < quests.Definitions.Count; index++)
			{
				var objective = quests.Definitions[index].Objective;
				switch (objective.Kind)
				{
					case QuestObjectiveKind.CompleteDungeon:
						var dungeonExists = false;
						for (var preset = 0; preset < launchPresets.Presets.Count; preset++)
							dungeonExists |= launchPresets.Presets[preset].DungeonId == objective.TargetId;
						if (!dungeonExists) throw new InvalidOperationException(
							$"Quest '{quests.Definitions[index].QuestId}' references unknown dungeon '{objective.TargetId}'.");
						break;
					case QuestObjectiveKind.CollectResource:
						if (!items.TryGetResource(objective.TargetId, out _)) throw new InvalidOperationException(
							$"Quest '{quests.Definitions[index].QuestId}' references unknown resource '{objective.TargetId}'.");
						break;
					case QuestObjectiveKind.CompleteDialogue:
						var npcExists = false;
						for (var npc = 0; npc < guildHall.Npcs.Count; npc++) npcExists |= guildHall.Npcs[npc].NpcId == objective.TargetId;
						if (!npcExists) throw new InvalidOperationException(
							$"Quest '{quests.Definitions[index].QuestId}' references unknown NPC '{objective.TargetId}'.");
						break;
					default: throw new ArgumentOutOfRangeException(nameof(objective.Kind), objective.Kind, null);
				}
			}
		}

		private void CreateDeveloperConsole()
		{
			_developerConsoleController = new DeveloperRunConsoleController(
				_launchPresetCatalog,
				_dungeonRunTeamSetup,
				request => StartDeveloperRunAsync(request, CancellationToken).Forget(Debug.LogException),
				() => ReturnFromDeveloperRunAsync(CancellationToken).Forget(Debug.LogException));
			var consoleObject = new GameObject("DungeonRunDeveloperConsole");
			_developerConsoleView = consoleObject.AddComponent<DeveloperRunConsoleView>();
			_developerConsoleView.Initialize(_developerConsoleController);
		}

		private static IFeedbackService CreateFeedbackService(
			ITickHandler tickHandler,
			FeedbackRuntimeSettings settings)
		{
			AudioFeedbackPlayer audioPlayer = null;
			HapticFeedbackPlayer hapticPlayer = null;
			try
			{
				audioPlayer = new AudioFeedbackPlayer(settings.SfxVoiceLimit);
				hapticPlayer = new HapticFeedbackPlayer(
					tickHandler,
					settings.HapticImpulseLimit);
				return new FeedbackService(new IFeedbackPlayer[]
				{
					audioPlayer,
					hapticPlayer
				});
			}
			catch
			{
				hapticPlayer?.Dispose();
				audioPlayer?.Dispose();
				throw;
			}
		}
	}
}

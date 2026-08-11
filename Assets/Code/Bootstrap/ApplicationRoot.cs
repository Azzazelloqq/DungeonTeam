using System;
using System.Text;
using System.Threading;
using Azzazelloqq.Config;
using Code.Addressables.Generated;
using Code.Configuration;
using Code.MainMenu;
using Code.UI.MainMenu;
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
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using LightDI.Runtime;
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
		private MainMenuRoot _mainMenuRoot;
		private RewardCatalog _rewardCatalog;
		private DungeonRunTeamSetup _dungeonRunTeamSetup;
		private IFeedbackService _feedbackService;
		private DungeonRunRoot _dungeonRunRoot;
		private bool _isDungeonTransitioning;

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

			IResourceLoader resourceLoader = new AddressableResourceLoader();
			_uiService = new UIService.UIService(resourceLoader, _canvasContext);
			_globalContainer.RegisterAsSingleton<IUiService>(_uiService);
			_globalContainer.RegisterAsSingleton<IResourceLoader>(resourceLoader);

			await ShowLoadingScreenAsync(token);

			IConfig config = new Config(new ScriptableObjectConfigParser(_configCatalog));
			_globalContainer.RegisterAsSingleton<IConfig>(config);
			await config.InitializeAsync(token);
			IDungeonFactory dungeonFactory = new DungeonFactory(config.GetConfigPage<DungeonConfigPage>());
			_globalContainer.RegisterAsSingleton<IDungeonFactory>(dungeonFactory);
			var actorCatalog = config.GetConfigPage<ActorConfigPage>().CreateCatalog();
			_globalContainer.RegisterAsSingleton(actorCatalog);
			var combatCatalog = config.GetConfigPage<CombatConfigPage>().CreateCatalog();
			_globalContainer.RegisterAsSingleton(combatCatalog);
			ValidateActorCombatConfiguration(actorCatalog, combatCatalog);
			IActorDefinitionLoader actorDefinitionLoader =
				ActorDefinitionLoaderFactory.CreateActorDefinitionLoader();
			_globalContainer.RegisterAsSingleton<IActorDefinitionLoader>(actorDefinitionLoader);
			_dungeonRunTeamSetup = config
				.GetConfigPage<DungeonRunConfigPage>()
				.CreateTeamSetup(actorCatalog);
			_globalContainer.RegisterAsSingleton(_dungeonRunTeamSetup);
			IRewardPickupViewLoader rewardPickupViewLoader =
				RewardPickupViewLoaderFactory.CreateRewardPickupViewLoader();
			_globalContainer.RegisterAsSingleton<IRewardPickupViewLoader>(rewardPickupViewLoader);
			IChestViewLoader chestViewLoader = ChestViewLoaderFactory.CreateChestViewLoader();
			_globalContainer.RegisterAsSingleton<IChestViewLoader>(chestViewLoader);
			_rewardCatalog = config.GetConfigPage<RewardConfigPage>().CreateCatalog();
			_globalContainer.RegisterAsSingleton(_rewardCatalog);
			var enemyBehaviorCatalog = config
				.GetConfigPage<EnemyBehaviorConfigPage>()
				.CreateCatalog();
			_globalContainer.RegisterAsSingleton(enemyBehaviorCatalog);
			_globalContainer.RegisterAsSingleton(_teamControlSettings);
			_globalContainer.RegisterAsSingleton(_heroControlSettings);

			var dispatcher = new GameObject("TickHandlerDispatcher");
			var unityDispatcherBehaviour = dispatcher.AddComponent<UnityDispatcherBehaviour>();
			ITickHandler tickHandler = new UnityTickHandler(unityDispatcherBehaviour);

			_feedbackRuntimeSettings.Validate();
			_feedbackService = CreateFeedbackService(tickHandler, _feedbackRuntimeSettings);
			_globalContainer.RegisterAsSingleton<IFeedbackService>(_feedbackService);
			_globalContainer.RegisterAsSingleton<ITickHandler>(tickHandler);
			IMusicPlayer musicPlayer = new MusicPlayer();
			_globalContainer.RegisterAsSingleton<IMusicPlayer>(musicPlayer);
			var feedbackBankLoader = FeedbackBankLoaderFactory.CreateFeedbackBankLoader();
			_globalContainer.RegisterAsSingleton(feedbackBankLoader);

			_mainMenuRoot = MainMenuRootFactory.CreateMainMenuRoot(
				OnPlayRequested,
				OnBackRequested,
				Application.Quit);
			await _mainMenuRoot.InitializeAsync(token);

			await HideLoadingScreenAsync(token);
		}

		protected override void OnDispose()
		{
			DisposeDungeonRun();
			_rewardCatalog = null;
			_dungeonRunTeamSetup = null;

			_mainMenuRoot?.Dispose();
			_mainMenuRoot = null;

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
				_feedbackService = null;
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

		private void OnPlayRequested(MainMenuPlayRequest request)
		{
			if (_isDungeonTransitioning || _dungeonRunRoot != null)
			{
				return;
			}

			StartDungeonPreviewAsync(request, CancellationToken).Forget(Debug.LogException);
		}

		private static void ValidateActorCombatConfiguration(
			ActorConfigCatalog actorCatalog,
			CombatCatalog combatCatalog)
		{
			for (var actorIndex = 0;
			     actorIndex < actorCatalog.Definitions.Count;
			     actorIndex++)
			{
				var actor = actorCatalog.Definitions[actorIndex];
				combatCatalog.RequireLoadout(actor.CombatLoadoutId);
				for (var levelIndex = 0; levelIndex < actor.Levels.Count; levelIndex++)
				{
					var level = actor.Levels[levelIndex];
					combatCatalog.ResolvePrimaryAttack(
						actor.CombatLoadoutId,
						level.PrimaryAttackRank);
				}
			}
		}

		private async UniTask StartDungeonPreviewAsync(
			MainMenuPlayRequest request,
			CancellationToken token)
		{
			_isDungeonTransitioning = true;

			try
			{
				await ShowLoadingScreenAsync(token);

				_dungeonRunRoot = DungeonRunRootFactory.CreateDungeonRunRoot(
					CreateStartRequest(request),
					_dungeonRunBindings,
					_canvasContext.GetParent(UIElementGroup.OverlayElement),
					_worldCamera,
					new DesktopDungeonRunInput());
				await _dungeonRunRoot.InitializeAsync(token);
				_dungeonRunRoot.ProgressChanged += OnDungeonRunProgressChanged;
				_dungeonRunRoot.Finished += OnDungeonRunFinished;

				if (token.IsCancellationRequested)
				{
					token.ThrowIfCancellationRequested();
				}

				_mainMenuRoot.ShowDungeonPreview(CreatePreviewSummary(_dungeonRunRoot));
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				DisposeDungeonRun();
			}
			catch (Exception exception)
			{
				DisposeDungeonRun();
				Debug.LogException(exception);
				_mainMenuRoot.ShowSelection();
			}
			finally
			{
				try
				{
					if (_loadingScreen != null && !token.IsCancellationRequested)
					{
						await HideLoadingScreenAsync(token);
					}
				}
				finally
				{
					_isDungeonTransitioning = false;
				}
			}
		}

		private void OnBackRequested()
		{
			if (_isDungeonTransitioning)
			{
				return;
			}

			DisposeDungeonRun();
			_mainMenuRoot.ShowSelection();
		}

		private static string CreatePreviewSummary(DungeonRunRoot runRoot)
		{
			return $"{runRoot.MapSnapshot.DungeonId}\n" +
			       $"SEED: {runRoot.MapSnapshot.Seed}\n" +
			       $"ENEMIES: {runRoot.EnemyCount}\n" +
			       $"KILLED: {runRoot.KilledEnemyCount}\n" +
			       $"REWARDS: {runRoot.CollectedRewardCount}\n" +
			       $"EXIT: {(runRoot.CanExit ? "READY" : "LOCKED")}\n" +
			       $"PLANNED INTERESTS: {runRoot.ContentPlan.InterestPointSpawns.Count}\n" +
			       $"PLANNED OBJECTIVES: {runRoot.ContentPlan.ObjectiveSpawns.Count}";
		}

		private DungeonRunStartRequest CreateStartRequest(MainMenuPlayRequest request)
		{
			_dungeonRunTeamSetup.RequireValid(request.Team);
			return new DungeonRunStartRequest(
				new DungeonBuildRequest(
					request.DungeonId,
					"scenario.demo",
					"normal",
					request.Seed),
				request.Team);
		}

		private void OnDungeonRunProgressChanged()
		{
			if (_dungeonRunRoot != null)
			{
				_mainMenuRoot.ShowDungeonPreview(CreatePreviewSummary(_dungeonRunRoot));
			}
		}

		private void OnDungeonRunFinished(DungeonRunResult result)
		{
			_mainMenuRoot.ShowDungeonPreview(CreateResultSummary(result));
		}

		private string CreateResultSummary(DungeonRunResult result)
		{
			var summary = new StringBuilder()
				.Append(result.Outcome.ToString().ToUpperInvariant()).Append('\n')
				.Append(result.DungeonId).Append('\n')
				.Append("SEED: ").Append(result.Seed).Append('\n')
				.Append("KILLED: ").Append(result.KilledEnemies).Append('\n')
				.Append("REWARDS: ").Append(result.CollectedRewardCount);

			for (var index = 0; index < result.CollectedRewards.Count; index++)
			{
				var reward = result.CollectedRewards[index];
				var definition = _rewardCatalog.Require(reward.RewardId);
				summary.Append('\n')
					.Append(definition.DisplayName)
					.Append(": ")
					.Append(reward.Amount);
			}

			return summary.ToString();
		}

		private void DisposeDungeonRun()
		{
			var dungeonRunRoot = _dungeonRunRoot;
			_dungeonRunRoot = null;
			if (dungeonRunRoot != null)
			{
				dungeonRunRoot.ProgressChanged -= OnDungeonRunProgressChanged;
				dungeonRunRoot.Finished -= OnDungeonRunFinished;
			}

			dungeonRunRoot?.Dispose();
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

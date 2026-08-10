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
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
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
		private ITickHandler _tickHandler;
		private IFeedbackService _feedbackService;
		private IMusicPlayer _musicPlayer;
		private FeedbackBankLoader _feedbackBankLoader;
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
			_globalContainer.RegisterAsSingleton(_uiService);
			_globalContainer.RegisterAsSingleton(resourceLoader);

			await ShowLoadingScreenAsync(token);

			IConfig config = new Config(new ScriptableObjectConfigParser(_configCatalog));
			_globalContainer.RegisterAsSingleton(config);
			await config.InitializeAsync(token);
			_dungeonFactory = new DungeonFactory(config.GetConfigPage<DungeonConfigPage>());
			_actorCatalog = config.GetConfigPage<ActorConfigPage>().CreateCatalog();
			_skillCatalog = config.GetConfigPage<SkillConfigPage>().CreateCatalog();
			_skillViewLoader = new SkillViewLoader(_skillCatalog, resourceLoader);
			_actorDefinitionLoader = new ActorDefinitionLoader(_actorCatalog, resourceLoader);
			_dungeonRunTeamSetup = config
				.GetConfigPage<DungeonRunConfigPage>()
				.CreateTeamSetup(_actorCatalog, _skillCatalog);
			_rewardPickupViewLoader = new RewardPickupViewLoader(resourceLoader);
			_chestViewLoader = new ChestViewLoader(resourceLoader);
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
			_feedbackBankLoader = new FeedbackBankLoader(resourceLoader, _feedbackService);
			_globalContainer.RegisterAsSingleton(_feedbackBankLoader);

			_mainMenuRoot = new MainMenuRoot(
				_uiService,
				_dungeonRunTeamSetup,
				OnPlayRequested,
				OnBackRequested,
				Application.Quit);
			await _mainMenuRoot.InitializeAsync(token);

			await HideLoadingScreenAsync(token);
		}

		protected override void OnDispose()
		{
			DisposeDungeonRun();
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

		private void OnPlayRequested(MainMenuPlayRequest request)
		{
			if (_isDungeonTransitioning || _dungeonRunRoot != null)
			{
				return;
			}

			StartDungeonPreviewAsync(request, CancellationToken).Forget(Debug.LogException);
		}

		private async UniTask StartDungeonPreviewAsync(
			MainMenuPlayRequest request,
			CancellationToken token)
		{
			_isDungeonTransitioning = true;

			try
			{
				await ShowLoadingScreenAsync(token);

				_dungeonRunRoot = new DungeonRunRoot(
					_dungeonFactory,
					CreateStartRequest(request),
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
					new DesktopDungeonRunInput(),
					_rewardCatalog,
					_teamControlSettings,
					_heroControlSettings,
					_enemyBehaviorCatalog);
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

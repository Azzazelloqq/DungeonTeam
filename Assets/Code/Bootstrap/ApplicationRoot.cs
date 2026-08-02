using System;
using System.Threading;
using Azzazelloqq.Config;
using Code.Addressables.Generated;
using Code.Configuration;
using Code.MainMenu;
using Code.UI.MainMenu;
using Code.UI.LoadingScreen;
using Code.UIService;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
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

		private IDiContainer _globalContainer;
		private IUiService _uiService;
		private LoadingScreenViewBase _loadingScreen;
		private LoadingScreenViewModel _loadingScreenViewModel;
		private MainMenuRoot _mainMenuRoot;
		private IDungeonFactory _dungeonFactory;
		private ITickHandler _tickHandler;
		private DungeonRunRoot _dungeonRunRoot;
		private bool _isDungeonTransitioning;

		public ApplicationRoot(
			UICanvasContext canvasContext,
			ConfigCatalog configCatalog,
			Camera worldCamera,
			DungeonRunBindings dungeonRunBindings,
			TeamControlSettings teamControlSettings)
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

			var dispatcher = new GameObject("TickHandlerDispatcher");
			var unityDispatcherBehaviour = dispatcher.AddComponent<UnityDispatcherBehaviour>();
			_tickHandler = new UnityTickHandler(unityDispatcherBehaviour);
			_globalContainer.RegisterAsSingleton(_tickHandler);

			_mainMenuRoot = new MainMenuRoot(
				_uiService,
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

			_mainMenuRoot?.Dispose();
			_mainMenuRoot = null;

			_loadingScreenViewModel?.Dispose();
			_loadingScreenViewModel = null;
			_loadingScreen = null;

			_globalContainer?.Dispose();
			_globalContainer = null;
			_tickHandler = null;
			_uiService = null;
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
					new DungeonBuildRequest(
						request.DungeonId,
						"scenario.demo",
						"normal",
						request.Seed),
					_dungeonRunBindings,
					_worldCamera,
					_tickHandler,
					new DesktopTeamInput(),
					_teamControlSettings);
				await _dungeonRunRoot.InitializeAsync(token);

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
			       $"PLANNED INTERESTS: {runRoot.ContentPlan.InterestPointSpawns.Count}\n" +
			       $"PLANNED OBJECTIVES: {runRoot.ContentPlan.ObjectiveSpawns.Count}";
		}

		private void DisposeDungeonRun()
		{
			var dungeonRunRoot = _dungeonRunRoot;
			_dungeonRunRoot = null;
			dungeonRunRoot?.Dispose();
		}
	}
}

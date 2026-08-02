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
using DungeonTeam.Gameplay.Dungeon.Runtime.Config;
using DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure;
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

		private IDiContainer _globalContainer;
		private IUiService _uiService;
		private LoadingScreenViewBase _loadingScreen;
		private LoadingScreenViewModel _loadingScreenViewModel;
		private MainMenuRoot _mainMenuRoot;
		private IDungeonFactory _dungeonFactory;
		private IDungeonInstance _dungeonInstance;
		private bool _isDungeonTransitioning;

		public ApplicationRoot(
			UICanvasContext canvasContext,
			ConfigCatalog configCatalog,
			Camera worldCamera)
		{
			_canvasContext = canvasContext;
			_configCatalog = configCatalog;
			_worldCamera = worldCamera != null
				? worldCamera
				: throw new ArgumentNullException(nameof(worldCamera));
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
			ITickHandler tickHandler = new UnityTickHandler(unityDispatcherBehaviour);
			_globalContainer.RegisterAsSingleton(tickHandler);

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
			var dungeonInstance = _dungeonInstance;
			_dungeonInstance = null;
			dungeonInstance?.Dispose();
			_dungeonFactory = null;

			_mainMenuRoot?.Dispose();
			_mainMenuRoot = null;

			_loadingScreenViewModel?.Dispose();
			_loadingScreenViewModel = null;
			_loadingScreen = null;

			_globalContainer?.Dispose();
			_globalContainer = null;
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
			if (_isDungeonTransitioning || _dungeonInstance != null)
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

				var instance = await _dungeonFactory.CreateAsync(
					new DungeonBuildRequest(
						request.DungeonId,
						"scenario.demo",
						"normal",
						request.Seed),
					token);

				if (token.IsCancellationRequested)
				{
					instance.Dispose();
					token.ThrowIfCancellationRequested();
				}

				_dungeonInstance = instance;
				FrameDungeon();
				_mainMenuRoot.ShowDungeonPreview(CreatePreviewSummary(instance));
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
			}
			catch (Exception exception)
			{
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

			var dungeonInstance = _dungeonInstance;
			_dungeonInstance = null;

			try
			{
				dungeonInstance?.Dispose();
			}
			finally
			{
				_mainMenuRoot.ShowSelection();
			}
		}

		private static string CreatePreviewSummary(IDungeonInstance instance)
		{
			return $"{instance.MapSnapshot.DungeonId}\n" +
			       $"SEED: {instance.MapSnapshot.Seed}\n" +
			       $"ENEMIES: {instance.ContentPlan.EnemySpawns.Count}\n" +
			       $"INTERESTS: {instance.ContentPlan.InterestPointSpawns.Count}\n" +
			       $"OBJECTIVES: {instance.ContentPlan.ObjectiveSpawns.Count}";
		}

		private void FrameDungeon()
		{
			var renderers = UnityEngine.Object.FindObjectsByType<Renderer>();
			if (renderers.Length == 0)
			{
				throw new InvalidOperationException("Created dungeon has no visible geometry.");
			}

			var bounds = renderers[0].bounds;
			for (var index = 1; index < renderers.Length; index++)
			{
				bounds.Encapsulate(renderers[index].bounds);
			}

			var halfFieldOfView = _worldCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
			var distance = bounds.extents.magnitude / Mathf.Sin(halfFieldOfView) * 1.15f;
			var viewDirection = new Vector3(-0.65f, 1f, -0.65f).normalized;
			_worldCamera.transform.position = bounds.center + viewDirection * distance;
			_worldCamera.transform.LookAt(bounds.center);
		}
	}
}

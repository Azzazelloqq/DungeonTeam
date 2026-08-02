using System.Threading;
using Azzazelloqq.Config;
using Code.Addressables.Generated;
using Code.Configuration;
using Code.MainMenu;
using Code.UI.LoadingScreen;
using Code.UIService;
using Cysharp.Threading.Tasks;
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

		private IDiContainer _globalContainer;
		private IUiService _uiService;
		private LoadingScreenViewBase _loadingScreen;
		private LoadingScreenViewModel _loadingScreenViewModel;
		private MainMenuRoot _mainMenuRoot;

		public ApplicationRoot(UICanvasContext canvasContext, ConfigCatalog configCatalog)
		{
			_canvasContext = canvasContext;
			_configCatalog = configCatalog;
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

			var dispatcher = new GameObject("TickHandlerDispatcher");
			var unityDispatcherBehaviour = dispatcher.AddComponent<UnityDispatcherBehaviour>();
			ITickHandler tickHandler = new UnityTickHandler(unityDispatcherBehaviour);
			_globalContainer.RegisterAsSingleton(tickHandler);

			_mainMenuRoot = new MainMenuRoot(_uiService, OnPlayRequested, Application.Quit);
			await _mainMenuRoot.InitializeAsync(token);

			await HideLoadingScreenAsync(token);
		}

		protected override void OnDispose()
		{
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
			_loadingScreen = await _uiService.CreateAsync<LoadingScreenViewBase>(
				AddressableIds.UI.WindowsMainLoadingScreen,
				token: token);

			_loadingScreenViewModel = new LoadingScreenViewModel(new LoadingScreenModel());
			_loadingScreenViewModel.Initialize();
			_loadingScreen.Initialize(_loadingScreenViewModel, disposeWithViewModel: false);

			await _uiService.ShowAsync(_loadingScreen, token);
		}

		private async UniTask HideLoadingScreenAsync(CancellationToken token)
		{
			await _uiService.HideAsync(_loadingScreen, token);
			_loadingScreen = null;

			_loadingScreenViewModel.Dispose();
			_loadingScreenViewModel = null;
		}

		private static void OnPlayRequested()
		{
		}
	}
}

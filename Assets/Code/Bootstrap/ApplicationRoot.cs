using System.Threading;
using Azzazelloqq.Config;
using Code.Configuration;
using Code.MainMenu;
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

		public ApplicationRoot(UICanvasContext canvasContext, ConfigCatalog configCatalog)
		{
			_canvasContext = canvasContext;
			_configCatalog = configCatalog;
		}

		protected override async UniTask OnInitializeAsync(CancellationToken token)
		{
			_globalContainer = DiContainerFactory.CreateGlobalContainer();

			IConfig config = new Config(new ScriptableObjectConfigParser(_configCatalog));
			_globalContainer.RegisterAsSingleton(config);
			await config.InitializeAsync(token);

			IResourceLoader resourceLoader = new AddressableResourceLoader();
			_globalContainer.RegisterAsSingleton(resourceLoader);

			var dispatcher = new GameObject("TickHandlerDispatcher");
			var unityDispatcherBehaviour = dispatcher.AddComponent<UnityDispatcherBehaviour>();
			ITickHandler tickHandler = new UnityTickHandler(unityDispatcherBehaviour);
			_globalContainer.RegisterAsSingleton(tickHandler);

			var uiService = new UIService.UIService(resourceLoader, _canvasContext);
			_globalContainer.RegisterAsSingleton<IUiService>(uiService);

			var root = MainMenuRootFactory.CreateMainMenuRoot();
			await root.InitializeAsync(token);
		}

		protected override void OnDispose()
		{
			base.OnDispose();

			_globalContainer?.Dispose();
		}
	}
}

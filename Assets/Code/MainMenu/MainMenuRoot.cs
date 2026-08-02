using System;
using System.Threading;
using Code.Addressables.Generated;
using Code.UI.MainMenu;
using Code.UIService;
using Cysharp.Threading.Tasks;
using RootPattern;

namespace Code.MainMenu
{
    public sealed class MainMenuRoot : Root
    {
        private readonly IUiService _uiService;
        private readonly Action<MainMenuPlayRequest> _playRequested;
        private readonly Action _backRequested;
        private readonly Action _quitConfirmed;

        private MainMenuViewModel _viewModel;

        public MainMenuRoot(
            IUiService uiService,
            Action<MainMenuPlayRequest> playRequested,
            Action backRequested,
            Action quitConfirmed)
        {
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            _playRequested = playRequested ?? throw new ArgumentNullException(nameof(playRequested));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            _quitConfirmed = quitConfirmed ?? throw new ArgumentNullException(nameof(quitConfirmed));
        }

        protected override async UniTask OnInitializeAsync(CancellationToken token)
        {
            var view = await _uiService.CreateAsync<MainMenuViewBase>(
                AddressableIds.UI.WindowsMainMainMenuPrefab,
                token: token);

            _viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                _playRequested,
                _backRequested,
                _quitConfirmed);
            _viewModel.Initialize();
            view.Initialize(_viewModel, disposeWithViewModel: false);

            await _uiService.ShowAsync(view, token);
        }

        public void ShowDungeonPreview(string summary)
        {
            _viewModel.ShowPreview(summary);
        }

        public void ShowSelection()
        {
            _viewModel.ShowSelection();
        }

        private static MainMenuDungeonOption[] CreateDungeonOptions()
        {
            return new[]
            {
                new MainMenuDungeonOption("AUTHORED", "dungeon.demo.authored"),
                new MainMenuDungeonOption("CHUNKED", "dungeon.demo.chunked"),
                new MainMenuDungeonOption("PROCEDURAL", "dungeon.demo.procedural")
            };
        }

        protected override void OnDispose()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }
    }
}

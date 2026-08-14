using System;
using System.Threading;
using Code.Addressables.Generated;
using Code.UI.MainMenu;
using Code.UIService;
using Cysharp.Threading.Tasks;
using LightDI.Runtime;
using RootPattern;

namespace Code.MainMenu
{
    public sealed class MainMenuRoot : Root
    {
        private readonly IUiService _uiService;
        private readonly Action _playRequested;
        private readonly Action _backRequested;
        private readonly Action _quitConfirmed;

        private MainMenuViewModel _viewModel;
        private MainMenuViewBase _view;

        public MainMenuRoot(
            [Inject] IUiService uiService,
            Action playRequested,
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
            _view = await _uiService.CreateAsync<MainMenuViewBase>(
                AddressableIds.UI.WindowsMainMainMenuPrefab,
                token: token);

            _viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                _playRequested,
                _backRequested,
                _quitConfirmed);
            _viewModel.Initialize();
            _view.Initialize(_viewModel, disposeWithViewModel: false);

            await _uiService.ShowAsync(_view, token);
        }

        public void ShowSelection()
        {
            _viewModel.ShowSelection();
        }

        public async UniTask HideAsync(CancellationToken token)
        {
            await _uiService.HideAsync(_view, token);
        }

        public async UniTask ShowTerminalAsync(string summary, CancellationToken token)
        {
            _viewModel.ShowPreview(summary);
            await _uiService.ShowAsync(_view, token);
        }

        public async UniTask ShowSelectionAsync(CancellationToken token)
        {
            _viewModel.ShowSelection();
            await _uiService.ShowAsync(_view, token);
        }

        protected override void OnDispose()
        {
            _viewModel?.Dispose();
            _viewModel = null;
            _view = null;
        }
    }
}

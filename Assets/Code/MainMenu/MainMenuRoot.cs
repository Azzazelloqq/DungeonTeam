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
            var view = await _uiService.CreateAsync<MainMenuViewBase>(
                AddressableIds.UI.WindowsMainMainMenuPrefab,
                token: token);

            _viewModel = new MainMenuViewModel(
                new MainMenuModel(),
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

        protected override void OnDispose()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }
    }
}

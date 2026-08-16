using System;
using System.Threading;
using Code.Addressables.Generated;
using Code.UIService;
using Cysharp.Threading.Tasks;
using RootPattern;

namespace DungeonTeam.UI.WorldMap
{
    public sealed class WorldMapRoot : Root
    {
        private readonly IUiService _uiService;
        private readonly WorldMapStartContext _context;
        private readonly Action<string> _locationSelected;
        private readonly Action _backRequested;
        private WorldMapView _view;
        private WorldMapViewModel _viewModel;
        private bool _isClosed;

        public WorldMapRoot(IUiService uiService, WorldMapStartContext context, Action<string> locationSelected, Action backRequested)
        {
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locationSelected = locationSelected ?? throw new ArgumentNullException(nameof(locationSelected));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
        }

        public WorldMapViewModel ViewModel => _viewModel;

        protected override async UniTask OnInitializeAsync(CancellationToken token)
        {
            try
            {
                _view = await _uiService.CreateAsync<WorldMapView>(AddressableIds.UI.WindowsWorldMapWorldMap, token: token);
                _viewModel = new WorldMapViewModel(_context, _locationSelected, _backRequested);
                _view.Initialize(_viewModel);
            }
            catch
            {
                if (_view != null && !_isClosed)
                {
                    _isClosed = true;
                    await _uiService.CloseAsync(_view, CancellationToken.None);
                }
                throw;
            }
        }

        public UniTask ShowAsync(CancellationToken token) => _uiService.ShowAsync(_view, token);

        public void RestoreInteraction()
        {
            if (_viewModel?.RestoreInteraction() == true)
            {
                _view?.RefreshInteractionState();
            }
        }

        public async UniTask CloseAsync(CancellationToken token)
        {
            if (_isClosed) throw new InvalidOperationException("World Map has already been closed.");
            _isClosed = true;
            try
            {
                await _uiService.CloseAsync(_view, token);
            }
            catch
            {
                _isClosed = false;
                throw;
            }
        }

        protected override void OnDispose()
        {
            _viewModel?.Dispose();
            _viewModel = null;
            _view = null;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.MainMenu
{
    public sealed class MainMenuViewModel : MainMenuViewModelBase
    {
        private readonly Action _playRequested;
        private readonly Action _backRequested;
        private readonly Action _quitConfirmed;
        private readonly ReactiveProperty<bool> _isQuitConfirmationVisible = new();
        private readonly ReactiveProperty<bool> _isPreviewVisible = new();
        private readonly ReactiveProperty<bool> _canPlay = new();
        private readonly ReactiveProperty<string> _previewSummary = new();

        public MainMenuViewModel(
            MainMenuModelBase model,
            Action playRequested,
            Action backRequested,
            Action quitConfirmed) : base(model)
        {
            _playRequested = playRequested ?? throw new ArgumentNullException(nameof(playRequested));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            _quitConfirmed = quitConfirmed ?? throw new ArgumentNullException(nameof(quitConfirmed));
            IsQuitConfirmationVisible = _isQuitConfirmationVisible;
            IsPreviewVisible = _isPreviewVisible;
            CanPlay = _canPlay;
            PreviewSummary = _previewSummary;
            PlayCommand = new ActionCommand(OnPlayRequested, CanPlayNow);
            BackCommand = new ActionCommand(OnBackRequested, IsPreviewVisibleNow);
            RequestQuitCommand = new ActionCommand(ShowQuitConfirmation);
            ConfirmQuitCommand = new ActionCommand(OnQuitConfirmed, CanConfirmQuit);
            CancelQuitCommand = new ActionCommand(HideQuitConfirmation, CanConfirmQuit);

            _isQuitConfirmationVisible.AddTo(compositeDisposable);
            _isPreviewVisible.AddTo(compositeDisposable);
            _canPlay.AddTo(compositeDisposable);
            _previewSummary.AddTo(compositeDisposable);
            PlayCommand.AddTo(compositeDisposable);
            BackCommand.AddTo(compositeDisposable);
            RequestQuitCommand.AddTo(compositeDisposable);
            ConfirmQuitCommand.AddTo(compositeDisposable);
            CancelQuitCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<bool> IsQuitConfirmationVisible { get; }

        public override IReadOnlyReactiveProperty<bool> IsPreviewVisible { get; }

        public override IReadOnlyReactiveProperty<bool> CanPlay { get; }

        public override IReadOnlyReactiveProperty<string> PreviewSummary { get; }

        public override IActionCommand PlayCommand { get; }

        public override IActionCommand BackCommand { get; }

        public override IActionCommand RequestQuitCommand { get; }

        public override IActionCommand ConfirmQuitCommand { get; }

        public override IActionCommand CancelQuitCommand { get; }

        protected override void OnInitialize()
        {
            UpdateCanPlay();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        public void ShowPreview(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException(
                    "Preview summary cannot be empty.",
                    nameof(summary));
            }

            _previewSummary.SetValue(summary);
            _isQuitConfirmationVisible.SetValue(false);
            _isPreviewVisible.SetValue(true);
            UpdateCanPlay();
        }

        public void ShowSelection()
        {
            _isPreviewVisible.SetValue(false);
            _previewSummary.SetValue(string.Empty);
            UpdateCanPlay();
        }

        private void OnPlayRequested()
        {
            _playRequested();
        }

        private void OnBackRequested()
        {
            _backRequested();
        }

        private bool IsPreviewVisibleNow()
        {
            return _isPreviewVisible.Value;
        }

        private bool CanPlayNow()
        {
            return !_isPreviewVisible.Value &&
                   !_isQuitConfirmationVisible.Value;
        }

        private void UpdateCanPlay()
        {
            _canPlay.SetValue(CanPlayNow());
        }

        private void ShowQuitConfirmation()
        {
            _isQuitConfirmationVisible.SetValue(true);
            UpdateCanPlay();
        }

        private void OnQuitConfirmed()
        {
            _quitConfirmed();
        }

        private bool CanConfirmQuit()
        {
            return _isQuitConfirmationVisible.Value;
        }

        private void HideQuitConfirmation()
        {
            _isQuitConfirmationVisible.SetValue(false);
            UpdateCanPlay();
        }
    }
}

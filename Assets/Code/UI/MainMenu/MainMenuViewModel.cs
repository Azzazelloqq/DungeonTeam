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
        private readonly Action _quitConfirmed;
        private readonly ReactiveProperty<bool> _isQuitConfirmationVisible = new();

        public MainMenuViewModel(
            MainMenuModelBase model,
            Action playRequested,
            Action quitConfirmed) : base(model)
        {
            _playRequested = playRequested ?? throw new ArgumentNullException(nameof(playRequested));
            _quitConfirmed = quitConfirmed ?? throw new ArgumentNullException(nameof(quitConfirmed));

            IsQuitConfirmationVisible = _isQuitConfirmationVisible;
            PlayCommand = new ActionCommand(OnPlayRequested);
            RequestQuitCommand = new ActionCommand(ShowQuitConfirmation);
            ConfirmQuitCommand = new ActionCommand(OnQuitConfirmed, CanConfirmQuit);
            CancelQuitCommand = new ActionCommand(HideQuitConfirmation, CanConfirmQuit);

            _isQuitConfirmationVisible.AddTo(compositeDisposable);
            PlayCommand.AddTo(compositeDisposable);
            RequestQuitCommand.AddTo(compositeDisposable);
            ConfirmQuitCommand.AddTo(compositeDisposable);
            CancelQuitCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<bool> IsQuitConfirmationVisible { get; }

        public override IActionCommand PlayCommand { get; }

        public override IActionCommand RequestQuitCommand { get; }

        public override IActionCommand ConfirmQuitCommand { get; }

        public override IActionCommand CancelQuitCommand { get; }

        protected override void OnInitialize()
        {
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

        private void OnPlayRequested()
        {
            _playRequested();
        }

        private void ShowQuitConfirmation()
        {
            _isQuitConfirmationVisible.SetValue(true);
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
        }
    }
}

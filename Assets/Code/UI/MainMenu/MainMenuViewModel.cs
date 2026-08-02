using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.MainMenu
{
    public sealed class MainMenuViewModel : MainMenuViewModelBase
    {
        private readonly IReadOnlyList<MainMenuDungeonOption> _dungeonOptions;
        private readonly Action<MainMenuPlayRequest> _playRequested;
        private readonly Action _backRequested;
        private readonly Action _quitConfirmed;
        private readonly ReactiveProperty<bool> _isQuitConfirmationVisible = new();
        private readonly ReactiveProperty<bool> _isPreviewVisible = new();
        private readonly ReactiveProperty<string> _selectedDungeonLabel = new();
        private readonly ReactiveProperty<string> _seedLabel = new();
        private readonly ReactiveProperty<string> _previewSummary = new();

        private int _selectedDungeonIndex;
        private int _seed = 42;

        public MainMenuViewModel(
            MainMenuModelBase model,
            IReadOnlyList<MainMenuDungeonOption> dungeonOptions,
            Action<MainMenuPlayRequest> playRequested,
            Action backRequested,
            Action quitConfirmed) : base(model)
        {
            if (dungeonOptions == null || dungeonOptions.Count == 0)
            {
                throw new ArgumentException("At least one dungeon option is required.", nameof(dungeonOptions));
            }

            _dungeonOptions = dungeonOptions;
            _playRequested = playRequested ?? throw new ArgumentNullException(nameof(playRequested));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            _quitConfirmed = quitConfirmed ?? throw new ArgumentNullException(nameof(quitConfirmed));

            IsQuitConfirmationVisible = _isQuitConfirmationVisible;
            IsPreviewVisible = _isPreviewVisible;
            SelectedDungeonLabel = _selectedDungeonLabel;
            SeedLabel = _seedLabel;
            PreviewSummary = _previewSummary;
            PlayCommand = new ActionCommand(OnPlayRequested, IsSelectionVisible);
            SelectNextDungeonCommand = new ActionCommand(SelectNextDungeon, IsSelectionVisible);
            DecreaseSeedCommand = new ActionCommand(DecreaseSeed, IsSelectionVisible);
            IncreaseSeedCommand = new ActionCommand(IncreaseSeed, IsSelectionVisible);
            BackCommand = new ActionCommand(OnBackRequested, IsPreviewVisibleNow);
            RequestQuitCommand = new ActionCommand(ShowQuitConfirmation);
            ConfirmQuitCommand = new ActionCommand(OnQuitConfirmed, CanConfirmQuit);
            CancelQuitCommand = new ActionCommand(HideQuitConfirmation, CanConfirmQuit);

            _isQuitConfirmationVisible.AddTo(compositeDisposable);
            _isPreviewVisible.AddTo(compositeDisposable);
            _selectedDungeonLabel.AddTo(compositeDisposable);
            _seedLabel.AddTo(compositeDisposable);
            _previewSummary.AddTo(compositeDisposable);
            PlayCommand.AddTo(compositeDisposable);
            SelectNextDungeonCommand.AddTo(compositeDisposable);
            DecreaseSeedCommand.AddTo(compositeDisposable);
            IncreaseSeedCommand.AddTo(compositeDisposable);
            BackCommand.AddTo(compositeDisposable);
            RequestQuitCommand.AddTo(compositeDisposable);
            ConfirmQuitCommand.AddTo(compositeDisposable);
            CancelQuitCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<bool> IsQuitConfirmationVisible { get; }

        public override IReadOnlyReactiveProperty<bool> IsPreviewVisible { get; }

        public override IReadOnlyReactiveProperty<string> SelectedDungeonLabel { get; }

        public override IReadOnlyReactiveProperty<string> SeedLabel { get; }

        public override IReadOnlyReactiveProperty<string> PreviewSummary { get; }

        public override IActionCommand PlayCommand { get; }

        public override IActionCommand SelectNextDungeonCommand { get; }

        public override IActionCommand DecreaseSeedCommand { get; }

        public override IActionCommand IncreaseSeedCommand { get; }

        public override IActionCommand BackCommand { get; }

        public override IActionCommand RequestQuitCommand { get; }

        public override IActionCommand ConfirmQuitCommand { get; }

        public override IActionCommand CancelQuitCommand { get; }

        protected override void OnInitialize()
        {
            UpdateSelectionLabels();
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
            var option = _dungeonOptions[_selectedDungeonIndex];
            _playRequested(new MainMenuPlayRequest(option.DungeonId, _seed));
        }

        public void ShowPreview(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException("Preview summary cannot be empty.", nameof(summary));
            }

            _previewSummary.SetValue(summary);
            _isQuitConfirmationVisible.SetValue(false);
            _isPreviewVisible.SetValue(true);
        }

        public void ShowSelection()
        {
            _isPreviewVisible.SetValue(false);
            _previewSummary.SetValue(string.Empty);
        }

        private void SelectNextDungeon()
        {
            _selectedDungeonIndex = (_selectedDungeonIndex + 1) % _dungeonOptions.Count;
            UpdateSelectionLabels();
        }

        private void DecreaseSeed()
        {
            _seed = unchecked(_seed - 1);
            UpdateSelectionLabels();
        }

        private void IncreaseSeed()
        {
            _seed = unchecked(_seed + 1);
            UpdateSelectionLabels();
        }

        private void UpdateSelectionLabels()
        {
            _selectedDungeonLabel.SetValue($"DUNGEON: {_dungeonOptions[_selectedDungeonIndex].DisplayName}");
            _seedLabel.SetValue($"SEED: {_seed}");
        }

        private void OnBackRequested()
        {
            _backRequested();
        }

        private bool IsSelectionVisible()
        {
            return !_isPreviewVisible.Value;
        }

        private bool IsPreviewVisibleNow()
        {
            return _isPreviewVisible.Value;
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

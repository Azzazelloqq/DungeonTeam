using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UI.MainMenu.TeamMemberSelection;
using Code.UI.MainMenu.TeamMemberSelection.Base;
using DungeonTeam.Gameplay.DungeonRun.Application;

namespace Code.UI.MainMenu
{
    public sealed class MainMenuViewModel : MainMenuViewModelBase
    {
        private readonly IReadOnlyList<MainMenuDungeonOption> _dungeonOptions;
        private readonly DungeonRunTeamSetup _teamSetup;
        private readonly Action<MainMenuPlayRequest> _playRequested;
        private readonly Action _backRequested;
        private readonly Action _quitConfirmed;
        private readonly HashSet<string> _selectedCompanionIds =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _selectedLevels =
            new(StringComparer.Ordinal);
        private readonly List<MainMenuTeamMemberViewModelBase> _teamMembers = new();
        private readonly ReactiveProperty<bool> _isQuitConfirmationVisible = new();
        private readonly ReactiveProperty<bool> _isPreviewVisible = new();
        private readonly ReactiveProperty<bool> _canPlay = new();
        private readonly ReactiveProperty<string> _selectedDungeonLabel = new();
        private readonly ReactiveProperty<string> _seedLabel = new();
        private readonly ReactiveProperty<string> _previewSummary = new();
        private readonly ReactiveProperty<string> _teamSummary = new();

        private string _selectedLeaderActorId;
        private int _selectedDungeonIndex;
        private int _seed = 42;

        public MainMenuViewModel(
            MainMenuModelBase model,
            IReadOnlyList<MainMenuDungeonOption> dungeonOptions,
            DungeonRunTeamSetup teamSetup,
            Action<MainMenuPlayRequest> playRequested,
            Action backRequested,
            Action quitConfirmed) : base(model)
        {
            if (dungeonOptions == null || dungeonOptions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one dungeon option is required.",
                    nameof(dungeonOptions));
            }

            _dungeonOptions = dungeonOptions;
            _teamSetup = teamSetup ?? throw new ArgumentNullException(nameof(teamSetup));
            _playRequested = playRequested ?? throw new ArgumentNullException(nameof(playRequested));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            _quitConfirmed = quitConfirmed ?? throw new ArgumentNullException(nameof(quitConfirmed));

            _selectedLeaderActorId = _teamSetup.DefaultSelection.LeaderActorId;
            for (var index = 0;
                 index < _teamSetup.DefaultSelection.Companions.Count;
                 index++)
            {
                _selectedCompanionIds.Add(
                    _teamSetup.DefaultSelection.Companions[index].ActorId);
            }

            for (var index = 0; index < _teamSetup.Members.Count; index++)
            {
                var option = _teamSetup.Members[index];
                var initialLevel = GetDefaultLevel(option);
                _selectedLevels.Add(option.ActorId, initialLevel);
                var member = new MainMenuTeamMemberViewModel(
                    new MainMenuTeamMemberModel(),
                    option.ActorId,
                    option.DisplayName,
                    option.AvailableLevels,
                    initialLevel,
                    SelectLeader,
                    ToggleCompanion,
                    SetLevel);
                _teamMembers.Add(member);
                compositeDisposable.AddDisposable(member);
            }

            TeamMembers = _teamMembers;
            IsQuitConfirmationVisible = _isQuitConfirmationVisible;
            IsPreviewVisible = _isPreviewVisible;
            CanPlay = _canPlay;
            SelectedDungeonLabel = _selectedDungeonLabel;
            SeedLabel = _seedLabel;
            PreviewSummary = _previewSummary;
            TeamSummary = _teamSummary;
            PlayCommand = new ActionCommand(OnPlayRequested, CanPlayNow);
            SelectNextDungeonCommand = new ActionCommand(SelectNextDungeon, IsSelectionVisible);
            DecreaseSeedCommand = new ActionCommand(DecreaseSeed, IsSelectionVisible);
            IncreaseSeedCommand = new ActionCommand(IncreaseSeed, IsSelectionVisible);
            BackCommand = new ActionCommand(OnBackRequested, IsPreviewVisibleNow);
            RequestQuitCommand = new ActionCommand(ShowQuitConfirmation);
            ConfirmQuitCommand = new ActionCommand(OnQuitConfirmed, CanConfirmQuit);
            CancelQuitCommand = new ActionCommand(HideQuitConfirmation, CanConfirmQuit);

            _isQuitConfirmationVisible.AddTo(compositeDisposable);
            _isPreviewVisible.AddTo(compositeDisposable);
            _canPlay.AddTo(compositeDisposable);
            _selectedDungeonLabel.AddTo(compositeDisposable);
            _seedLabel.AddTo(compositeDisposable);
            _previewSummary.AddTo(compositeDisposable);
            _teamSummary.AddTo(compositeDisposable);
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

        public override IReadOnlyReactiveProperty<bool> CanPlay { get; }

        public override IReadOnlyReactiveProperty<string> SelectedDungeonLabel { get; }

        public override IReadOnlyReactiveProperty<string> SeedLabel { get; }

        public override IReadOnlyReactiveProperty<string> PreviewSummary { get; }

        public override IReadOnlyReactiveProperty<string> TeamSummary { get; }

        public override IReadOnlyList<MainMenuTeamMemberViewModelBase> TeamMembers { get; }

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
            for (var index = 0; index < _teamMembers.Count; index++)
            {
                _teamMembers[index].Initialize();
            }

            UpdateSelectionState();
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
            var team = CreateTeamSelection();
            _teamSetup.RequireValid(team);
            var option = _dungeonOptions[_selectedDungeonIndex];
            _playRequested(new MainMenuPlayRequest(option.DungeonId, _seed, team));
        }

        private void SelectLeader(string actorId)
        {
            if (string.Equals(
                    actorId,
                    _selectedLeaderActorId,
                    StringComparison.Ordinal))
            {
                return;
            }

            var previousLeader = _selectedLeaderActorId;
            _selectedCompanionIds.Remove(actorId);
            _selectedCompanionIds.Add(previousLeader);
            _selectedLeaderActorId = actorId;
            UpdateSelectionState();
        }

        private void ToggleCompanion(string actorId)
        {
            if (string.Equals(
                    actorId,
                    _selectedLeaderActorId,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (!_selectedCompanionIds.Remove(actorId))
            {
                if (_selectedCompanionIds.Count + 1 >= _teamSetup.MaximumTeamSize)
                {
                    return;
                }

                _selectedCompanionIds.Add(actorId);
            }

            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            var teamSize = _selectedCompanionIds.Count + 1;
            for (var index = 0; index < _teamMembers.Count; index++)
            {
                var member = _teamMembers[index];
                var isLeader = string.Equals(
                    member.ActorId,
                    _selectedLeaderActorId,
                    StringComparison.Ordinal);
                var isCompanion = _selectedCompanionIds.Contains(member.ActorId);
                member.SetSelectionState(
                    isLeader,
                    isCompanion,
                    !isLeader &&
                    (isCompanion || teamSize < _teamSetup.MaximumTeamSize));
            }

            _teamSummary.SetValue(
                $"TEAM: {teamSize} / {_teamSetup.MaximumTeamSize}");
            UpdateSelectionLabels();
        }

        private DungeonRunTeamSelection CreateTeamSelection()
        {
            var companions = new List<DungeonRunActorSelection>(_selectedCompanionIds.Count);
            for (var index = 0; index < _teamSetup.Members.Count; index++)
            {
                var actorId = _teamSetup.Members[index].ActorId;
                if (_selectedCompanionIds.Contains(actorId))
                {
                    companions.Add(new DungeonRunActorSelection(
                        actorId,
                        _selectedLevels[actorId]));
                }
            }

            return new DungeonRunTeamSelection(
                new DungeonRunActorSelection(
                    _selectedLeaderActorId,
                    _selectedLevels[_selectedLeaderActorId]),
                companions);
        }

        private int GetDefaultLevel(DungeonRunTeamMemberOption option)
        {
            if (string.Equals(
                    option.ActorId,
                    _teamSetup.DefaultSelection.Leader.ActorId,
                    StringComparison.Ordinal))
            {
                return _teamSetup.DefaultSelection.Leader.Level;
            }

            for (var index = 0;
                 index < _teamSetup.DefaultSelection.Companions.Count;
                 index++)
            {
                var companion = _teamSetup.DefaultSelection.Companions[index];
                if (string.Equals(option.ActorId, companion.ActorId, StringComparison.Ordinal))
                {
                    return companion.Level;
                }
            }

            return option.AvailableLevels[0];
        }

        private void SetLevel(string actorId, int level)
        {
            _selectedLevels[actorId] = level;
            UpdateCanPlay();
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
            _selectedDungeonLabel.SetValue(
                $"DUNGEON: {_dungeonOptions[_selectedDungeonIndex].DisplayName}");
            _seedLabel.SetValue($"SEED: {_seed}");
            UpdateCanPlay();
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

        private bool CanPlayNow()
        {
            return !_isPreviewVisible.Value &&
                   !_isQuitConfirmationVisible.Value &&
                   _teamSetup.IsValid(CreateTeamSelection());
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

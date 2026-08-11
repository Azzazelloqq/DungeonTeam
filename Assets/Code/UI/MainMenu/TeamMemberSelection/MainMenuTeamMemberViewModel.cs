using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UI.MainMenu.TeamMemberSelection.Base;

namespace Code.UI.MainMenu.TeamMemberSelection
{
    public sealed class MainMenuTeamMemberViewModel :
        MainMenuTeamMemberViewModelBase
    {
        private readonly Action<string> _selectLeader;
        private readonly Action<string> _toggleCompanion;
        private readonly Action<string, int> _levelChanged;
        private readonly Action<string, string> _loadoutChanged;
        private readonly int[] _availableLevels;
        private readonly string[] _availableLoadoutIds;
        private readonly ReactiveProperty<string> _label = new();
        private readonly ReactiveProperty<bool> _isLeader = new();
        private readonly ReactiveProperty<bool> _isCompanion = new();
        private readonly ReactiveProperty<bool> _canToggleCompanion = new();
        private readonly ReactiveProperty<string> _levelLabel = new();
        private readonly ReactiveProperty<bool> _canDecreaseLevel = new();
        private readonly ReactiveProperty<bool> _canIncreaseLevel = new();
        private readonly ReactiveProperty<string> _loadoutLabel = new();
        private readonly ReactiveProperty<bool> _canDecreaseLoadout = new();
        private readonly ReactiveProperty<bool> _canIncreaseLoadout = new();
        private readonly string _displayName;
        private int _levelIndex;
        private int _loadoutIndex;

        public MainMenuTeamMemberViewModel(
            MainMenuTeamMemberModelBase model,
            string actorId,
            string displayName,
            IReadOnlyList<int> availableLevels,
            int initialLevel,
            IReadOnlyList<string> availableLoadoutIds,
            string initialLoadoutId,
            Action<string> selectLeader,
            Action<string> toggleCompanion,
            Action<string, int> levelChanged,
            Action<string, string> loadoutChanged) : base(model)
        {
            ActorId = !string.IsNullOrWhiteSpace(actorId)
                ? actorId
                : throw new ArgumentException(
                    "Actor ID cannot be empty.",
                    nameof(actorId));
            _displayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : throw new ArgumentException(
                    "Display name cannot be empty.",
                    nameof(displayName));
            _selectLeader = selectLeader ?? throw new ArgumentNullException(nameof(selectLeader));
            _toggleCompanion = toggleCompanion ??
                throw new ArgumentNullException(nameof(toggleCompanion));
            _levelChanged = levelChanged ?? throw new ArgumentNullException(nameof(levelChanged));
            _loadoutChanged = loadoutChanged ??
                throw new ArgumentNullException(nameof(loadoutChanged));
            if (availableLevels == null || availableLevels.Count == 0)
            {
                throw new ArgumentException("Available levels are required.", nameof(availableLevels));
            }

            _availableLevels = new int[availableLevels.Count];
            _levelIndex = -1;
            for (var index = 0; index < availableLevels.Count; index++)
            {
                _availableLevels[index] = availableLevels[index];
                if (availableLevels[index] == initialLevel)
                {
                    _levelIndex = index;
                }
            }

            if (_levelIndex < 0)
            {
                throw new ArgumentException("Initial level is not available.", nameof(initialLevel));
            }

            if (availableLoadoutIds == null || availableLoadoutIds.Count == 0)
            {
                throw new ArgumentException(
                    "Available loadouts are required.",
                    nameof(availableLoadoutIds));
            }

            _availableLoadoutIds = new string[availableLoadoutIds.Count];
            _loadoutIndex = -1;
            for (var index = 0; index < availableLoadoutIds.Count; index++)
            {
                var loadoutId = availableLoadoutIds[index];
                if (string.IsNullOrWhiteSpace(loadoutId))
                {
                    throw new ArgumentException(
                        "Available loadout ID cannot be empty.",
                        nameof(availableLoadoutIds));
                }

                _availableLoadoutIds[index] = loadoutId;
                if (string.Equals(loadoutId, initialLoadoutId, StringComparison.Ordinal))
                {
                    _loadoutIndex = index;
                }
            }

            if (_loadoutIndex < 0)
            {
                throw new ArgumentException(
                    "Initial loadout is not available.",
                    nameof(initialLoadoutId));
            }

            Label = _label;
            IsLeader = _isLeader;
            IsCompanion = _isCompanion;
            CanToggleCompanion = _canToggleCompanion;
            LevelLabel = _levelLabel;
            CanDecreaseLevel = _canDecreaseLevel;
            CanIncreaseLevel = _canIncreaseLevel;
            LoadoutLabel = _loadoutLabel;
            CanDecreaseLoadout = _canDecreaseLoadout;
            CanIncreaseLoadout = _canIncreaseLoadout;
            SelectLeaderCommand = new ActionCommand(() => _selectLeader(ActorId));
            ToggleCompanionCommand = new ActionCommand(
                () => _toggleCompanion(ActorId),
                () => _canToggleCompanion.Value);
            DecreaseLevelCommand = new ActionCommand(DecreaseLevel, () => _canDecreaseLevel.Value);
            IncreaseLevelCommand = new ActionCommand(IncreaseLevel, () => _canIncreaseLevel.Value);
            DecreaseLoadoutCommand = new ActionCommand(
                DecreaseLoadout,
                () => _canDecreaseLoadout.Value);
            IncreaseLoadoutCommand = new ActionCommand(
                IncreaseLoadout,
                () => _canIncreaseLoadout.Value);

            _label.AddTo(compositeDisposable);
            _isLeader.AddTo(compositeDisposable);
            _isCompanion.AddTo(compositeDisposable);
            _canToggleCompanion.AddTo(compositeDisposable);
            _levelLabel.AddTo(compositeDisposable);
            _canDecreaseLevel.AddTo(compositeDisposable);
            _canIncreaseLevel.AddTo(compositeDisposable);
            _loadoutLabel.AddTo(compositeDisposable);
            _canDecreaseLoadout.AddTo(compositeDisposable);
            _canIncreaseLoadout.AddTo(compositeDisposable);
            SelectLeaderCommand.AddTo(compositeDisposable);
            ToggleCompanionCommand.AddTo(compositeDisposable);
            DecreaseLevelCommand.AddTo(compositeDisposable);
            IncreaseLevelCommand.AddTo(compositeDisposable);
            DecreaseLoadoutCommand.AddTo(compositeDisposable);
            IncreaseLoadoutCommand.AddTo(compositeDisposable);
            UpdateLevelState();
            UpdateLoadoutState();
        }

        public override string ActorId { get; }

        public override IReadOnlyReactiveProperty<string> Label { get; }

        public override IReadOnlyReactiveProperty<bool> IsLeader { get; }

        public override IReadOnlyReactiveProperty<bool> IsCompanion { get; }

        public override IReadOnlyReactiveProperty<bool> CanToggleCompanion { get; }

        public override IReadOnlyReactiveProperty<string> LevelLabel { get; }

        public override IReadOnlyReactiveProperty<bool> CanDecreaseLevel { get; }

        public override IReadOnlyReactiveProperty<bool> CanIncreaseLevel { get; }

        public override IReadOnlyReactiveProperty<string> LoadoutLabel { get; }

        public override IReadOnlyReactiveProperty<bool> CanDecreaseLoadout { get; }

        public override IReadOnlyReactiveProperty<bool> CanIncreaseLoadout { get; }

        public override IActionCommand SelectLeaderCommand { get; }

        public override IActionCommand ToggleCompanionCommand { get; }

        public override IActionCommand DecreaseLevelCommand { get; }

        public override IActionCommand IncreaseLevelCommand { get; }

        public override IActionCommand DecreaseLoadoutCommand { get; }

        public override IActionCommand IncreaseLoadoutCommand { get; }

        internal override void SetSelectionState(
            bool isLeader,
            bool isCompanion,
            bool canToggleCompanion)
        {
            _isLeader.SetValue(isLeader);
            _isCompanion.SetValue(isCompanion);
            _canToggleCompanion.SetValue(canToggleCompanion);
            var role = isLeader
                ? "LEADER"
                : isCompanion
                    ? "COMPANION"
                    : "AVAILABLE";
            _label.SetValue($"{_displayName} — {role}");
        }

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

        private void DecreaseLevel()
        {
            if (_levelIndex <= 0)
            {
                return;
            }

            _levelIndex--;
            PublishLevel();
        }

        private void IncreaseLevel()
        {
            if (_levelIndex >= _availableLevels.Length - 1)
            {
                return;
            }

            _levelIndex++;
            PublishLevel();
        }

        private void PublishLevel()
        {
            UpdateLevelState();
            _levelChanged(ActorId, _availableLevels[_levelIndex]);
        }

        private void UpdateLevelState()
        {
            _levelLabel.SetValue($"LVL {_availableLevels[_levelIndex]}");
            _canDecreaseLevel.SetValue(_levelIndex > 0);
            _canIncreaseLevel.SetValue(_levelIndex < _availableLevels.Length - 1);
        }

        private void DecreaseLoadout()
        {
            if (_loadoutIndex <= 0)
                return;

            _loadoutIndex--;
            PublishLoadout();
        }

        private void IncreaseLoadout()
        {
            if (_loadoutIndex >= _availableLoadoutIds.Length - 1)
                return;

            _loadoutIndex++;
            PublishLoadout();
        }

        private void PublishLoadout()
        {
            UpdateLoadoutState();
            _loadoutChanged(ActorId, _availableLoadoutIds[_loadoutIndex]);
        }

        private void UpdateLoadoutState()
        {
            _loadoutLabel.SetValue(_availableLoadoutIds[_loadoutIndex]);
            _canDecreaseLoadout.SetValue(_loadoutIndex > 0);
            _canIncreaseLoadout.SetValue(_loadoutIndex < _availableLoadoutIds.Length - 1);
        }
    }
}

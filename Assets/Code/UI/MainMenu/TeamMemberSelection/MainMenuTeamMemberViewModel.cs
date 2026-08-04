using System;
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
        private readonly ReactiveProperty<string> _label = new();
        private readonly ReactiveProperty<bool> _isLeader = new();
        private readonly ReactiveProperty<bool> _isCompanion = new();
        private readonly ReactiveProperty<bool> _canToggleCompanion = new();
        private readonly string _displayName;

        public MainMenuTeamMemberViewModel(
            MainMenuTeamMemberModelBase model,
            string actorId,
            string displayName,
            Action<string> selectLeader,
            Action<string> toggleCompanion) : base(model)
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

            Label = _label;
            IsLeader = _isLeader;
            IsCompanion = _isCompanion;
            CanToggleCompanion = _canToggleCompanion;
            SelectLeaderCommand = new ActionCommand(() => _selectLeader(ActorId));
            ToggleCompanionCommand = new ActionCommand(
                () => _toggleCompanion(ActorId),
                () => _canToggleCompanion.Value);

            _label.AddTo(compositeDisposable);
            _isLeader.AddTo(compositeDisposable);
            _isCompanion.AddTo(compositeDisposable);
            _canToggleCompanion.AddTo(compositeDisposable);
            SelectLeaderCommand.AddTo(compositeDisposable);
            ToggleCompanionCommand.AddTo(compositeDisposable);
        }

        public override string ActorId { get; }

        public override IReadOnlyReactiveProperty<string> Label { get; }

        public override IReadOnlyReactiveProperty<bool> IsLeader { get; }

        public override IReadOnlyReactiveProperty<bool> IsCompanion { get; }

        public override IReadOnlyReactiveProperty<bool> CanToggleCompanion { get; }

        public override IActionCommand SelectLeaderCommand { get; }

        public override IActionCommand ToggleCompanionCommand { get; }

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
    }
}

using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.MainMenu.TeamMemberSelection.Base
{
    public abstract class MainMenuTeamMemberViewModelBase :
        ViewModelBase<MainMenuTeamMemberModelBase>
    {
        protected MainMenuTeamMemberViewModelBase(
            MainMenuTeamMemberModelBase model) : base(model)
        {
        }

        public abstract string ActorId { get; }

        public abstract IReadOnlyReactiveProperty<string> Label { get; }

        public abstract IReadOnlyReactiveProperty<bool> IsLeader { get; }

        public abstract IReadOnlyReactiveProperty<bool> IsCompanion { get; }

        public abstract IReadOnlyReactiveProperty<bool> CanToggleCompanion { get; }

        public abstract IActionCommand SelectLeaderCommand { get; }

        public abstract IActionCommand ToggleCompanionCommand { get; }

        internal abstract void SetSelectionState(
            bool isLeader,
            bool isCompanion,
            bool canToggleCompanion);
    }
}

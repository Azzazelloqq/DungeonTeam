using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.MainMenu
{
    public abstract class MainMenuViewModelBase : ViewModelBase<MainMenuModelBase>
    {
        protected MainMenuViewModelBase(MainMenuModelBase model) : base(model)
        {
        }

        public abstract IReadOnlyReactiveProperty<bool> IsQuitConfirmationVisible { get; }

        public abstract IReadOnlyReactiveProperty<bool> IsPreviewVisible { get; }

        public abstract IReadOnlyReactiveProperty<string> SelectedDungeonLabel { get; }

        public abstract IReadOnlyReactiveProperty<string> SeedLabel { get; }

        public abstract IReadOnlyReactiveProperty<string> PreviewSummary { get; }

        public abstract IActionCommand PlayCommand { get; }

        public abstract IActionCommand SelectNextDungeonCommand { get; }

        public abstract IActionCommand DecreaseSeedCommand { get; }

        public abstract IActionCommand IncreaseSeedCommand { get; }

        public abstract IActionCommand BackCommand { get; }

        public abstract IActionCommand RequestQuitCommand { get; }

        public abstract IActionCommand ConfirmQuitCommand { get; }

        public abstract IActionCommand CancelQuitCommand { get; }
    }
}

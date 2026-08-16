using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary.Base
{
    public abstract class RunSummaryViewModelBase : ViewModelBase<RunSummaryModelBase>
    {
        protected RunSummaryViewModelBase(RunSummaryModelBase model) : base(model) { }
        public abstract GuildRunSummarySnapshot Summary { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
        public abstract void Open();
        public abstract void Close();
    }
}

using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary.Base
{
    public abstract class RunSummaryModelBase : ModelBase
    {
        public abstract GuildRunSummarySnapshot Summary { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract void Show();
        public abstract void Hide();
    }
}

using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base
{
    public abstract class GuildProfileViewModelBase : ViewModelBase<GuildProfileModelBase>
    {
        protected GuildProfileViewModelBase(GuildProfileModelBase model) : base(model) { }

        public abstract GuildProfileSnapshot Profile { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero { get; }
        public abstract IRelayCommand<string> SelectHeroCommand { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
        public abstract void Open();
        public abstract void Close();
    }
}

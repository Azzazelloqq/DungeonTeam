using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base
{
    public abstract class GuildProfileViewModelBase : ViewModelBase<GuildProfileModelBase>
    {
        protected GuildProfileViewModelBase(GuildProfileModelBase model) : base(model) { }

        public abstract GuildProfileSnapshot Profile { get; }
        public abstract IReadOnlyReactiveProperty<GuildProfileSnapshot> CurrentProfile { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero { get; }
        public abstract IReadOnlyReactiveProperty<GuildTextSnapshot> Rejection { get; }
        public abstract IRelayCommand<string> SelectHeroCommand { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
        public abstract IRelayCommand<object> SetLeaderCommand { get; }
        public abstract IRelayCommand<object> AddCompanionCommand { get; }
        public abstract IRelayCommand<object> RemoveCompanionCommand { get; }
        public abstract IRelayCommand<string> SetLoadoutCommand { get; }
        public abstract IRelayCommand<string> EquipItemCommand { get; }
        public abstract IRelayCommand<object> UnequipItemCommand { get; }
        public abstract IRelayCommand<string> SellUniqueItemCommand { get; }
        public abstract IRelayCommand<string> SellResourceCommand { get; }
        public abstract void Open();
        public abstract void Close();
    }
}

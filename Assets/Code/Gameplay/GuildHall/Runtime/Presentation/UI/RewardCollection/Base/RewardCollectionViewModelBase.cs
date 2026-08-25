using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection.Base
{
    public abstract class RewardCollectionViewModelBase : ViewModelBase<RewardCollectionModelBase>
    {
        protected RewardCollectionViewModelBase(RewardCollectionModelBase model) : base(model) { }
        public abstract RewardClaimPointSnapshot Point { get; }
        public abstract GuildTextSnapshot Header { get; }
        public abstract GuildTextSnapshot CloseText { get; }
        public abstract IReadOnlyList<RewardCollectionEntrySnapshot> Entries { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<int> Revision { get; }
        public abstract IRelayCommand<RewardClaimIdentity> ReceiveCommand { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
        public abstract void Open();
        public abstract void Close();
    }
}

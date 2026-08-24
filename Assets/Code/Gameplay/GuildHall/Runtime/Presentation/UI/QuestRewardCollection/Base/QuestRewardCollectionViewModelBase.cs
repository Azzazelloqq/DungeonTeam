using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base
{
    public abstract class QuestRewardCollectionViewModelBase : ViewModelBase<QuestRewardCollectionModelBase>
    {
        protected QuestRewardCollectionViewModelBase(QuestRewardCollectionModelBase model) : base(model) { }
        public abstract QuestRewardClaimPointSnapshot Point { get; }
        public abstract GuildTextSnapshot Header { get; }
        public abstract GuildTextSnapshot CloseText { get; }
        public abstract IReadOnlyList<QuestRewardCollectionEntrySnapshot> Entries { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<int> Revision { get; }
        public abstract IRelayCommand<string> ReceiveCommand { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
        public abstract void Open();
        public abstract void Close();
    }
}

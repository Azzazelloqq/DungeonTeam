using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base
{
    public abstract class QuestRewardCollectionModelBase : ModelBase
    {
        public abstract QuestRewardClaimPointSnapshot Point { get; }
        public abstract GuildTextSnapshot Header { get; }
        public abstract GuildTextSnapshot CloseText { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<int> Revision { get; }
        public abstract IReadOnlyList<QuestRewardCollectionEntrySnapshot> Entries { get; }
        public abstract void Show();
        public abstract void Hide();
        public abstract bool Remove(string questId);
    }
}

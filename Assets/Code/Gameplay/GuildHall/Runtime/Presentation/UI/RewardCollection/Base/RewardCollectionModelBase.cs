using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection.Base
{
    public abstract class RewardCollectionModelBase : ModelBase
    {
        public abstract RewardClaimPointSnapshot Point { get; }
        public abstract GuildTextSnapshot Header { get; }
        public abstract GuildTextSnapshot CloseText { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<int> Revision { get; }
        public abstract IReadOnlyList<RewardCollectionEntrySnapshot> Entries { get; }
        public abstract void Show();
        public abstract void Hide();
        public abstract bool Remove(RewardClaimIdentity identity);
    }
}

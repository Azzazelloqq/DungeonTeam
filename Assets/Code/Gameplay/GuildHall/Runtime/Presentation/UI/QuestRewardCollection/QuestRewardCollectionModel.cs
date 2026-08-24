using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection
{
    public sealed class QuestRewardCollectionModel : QuestRewardCollectionModelBase
    {
        private readonly ReactiveProperty<bool> _isVisible = new(false);
        private readonly ReactiveProperty<int> _revision = new(0);
        private readonly List<QuestRewardCollectionEntrySnapshot> _entries;

        public QuestRewardCollectionModel(QuestRewardCollectionSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _entries = new List<QuestRewardCollectionEntrySnapshot>(snapshot.Entries);
            _isVisible.AddTo(compositeDisposable);
            _revision.AddTo(compositeDisposable);
        }

        public QuestRewardCollectionSnapshot Snapshot { get; }
        public override QuestRewardClaimPointSnapshot Point => Snapshot.Point;
        public override GuildTextSnapshot Header => Snapshot.Header;
        public override GuildTextSnapshot CloseText => Snapshot.Close;
        public override IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public override IReadOnlyReactiveProperty<int> Revision => _revision;
        public override IReadOnlyList<QuestRewardCollectionEntrySnapshot> Entries =>
            new ReadOnlyCollection<QuestRewardCollectionEntrySnapshot>(_entries);
        public override void Show() => _isVisible.SetValue(true);
        public override void Hide() => _isVisible.SetValue(false);

        public override bool Remove(string questId)
        {
            for (var index = 0; index < _entries.Count; index++)
            {
                if (!string.Equals(_entries[index].QuestId, questId, StringComparison.Ordinal)) continue;
                _entries.RemoveAt(index);
                _revision.SetValue(_revision.Value + 1);
                return true;
            }
            return false;
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

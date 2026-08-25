using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection
{
    public sealed class RewardCollectionModel : RewardCollectionModelBase
    {
        private readonly ReactiveProperty<bool> _isVisible = new(false);
        private readonly ReactiveProperty<int> _revision = new(0);
        private readonly List<RewardCollectionEntrySnapshot> _entries;

        public RewardCollectionModel(RewardCollectionSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _entries = new List<RewardCollectionEntrySnapshot>(snapshot.Entries);
            _isVisible.AddTo(compositeDisposable);
            _revision.AddTo(compositeDisposable);
        }

        public RewardCollectionSnapshot Snapshot { get; }
        public override RewardClaimPointSnapshot Point => Snapshot.Point;
        public override GuildTextSnapshot Header => Snapshot.Header;
        public override GuildTextSnapshot CloseText => Snapshot.Close;
        public override IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public override IReadOnlyReactiveProperty<int> Revision => _revision;
        public override IReadOnlyList<RewardCollectionEntrySnapshot> Entries =>
            new ReadOnlyCollection<RewardCollectionEntrySnapshot>(_entries);
        public override void Show() => _isVisible.SetValue(true);
        public override void Hide() => _isVisible.SetValue(false);

        public override bool Remove(RewardClaimIdentity identity)
        {
            if (identity == null) return false;
            for (var index = 0; index < _entries.Count; index++)
            {
                if (!_entries[index].Identity.Matches(identity)) continue;
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

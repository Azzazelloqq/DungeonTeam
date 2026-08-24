using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection
{
    public sealed class QuestRewardCollectionViewModel : QuestRewardCollectionViewModelBase
    {
        private readonly Func<QuestRewardClaimRequest, bool> _claimRequested;
        private readonly Action _closed;

        public QuestRewardCollectionViewModel(
            QuestRewardCollectionModelBase model,
            Func<QuestRewardClaimRequest, bool> claimRequested,
            Action closed) : base(model)
        {
            _claimRequested = claimRequested ?? throw new ArgumentNullException(nameof(claimRequested));
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            ReceiveCommand = new RelayCommand<string>(Receive);
            CloseCommand = new RelayCommand<object>(_ => Close());
            ReceiveCommand.AddTo(compositeDisposable);
            CloseCommand.AddTo(compositeDisposable);
        }

        public override QuestRewardClaimPointSnapshot Point => model.Point;
        public override GuildTextSnapshot Header => model.Header;
        public override GuildTextSnapshot CloseText => model.CloseText;
        public override IReadOnlyList<QuestRewardCollectionEntrySnapshot> Entries => model.Entries;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IReadOnlyReactiveProperty<int> Revision => model.Revision;
        public override IRelayCommand<string> ReceiveCommand { get; }
        public override IRelayCommand<object> CloseCommand { get; }
        public override void Open() => model.Show();
        public override void Close()
        {
            if (model.IsVisible.Value) _closed();
        }

        private void Receive(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return;
            var request = new QuestRewardClaimRequest(questId, model.Point);
            if (_claimRequested(request)) model.Remove(questId);
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() => Close();
        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            Close();
            return default;
        }
    }
}

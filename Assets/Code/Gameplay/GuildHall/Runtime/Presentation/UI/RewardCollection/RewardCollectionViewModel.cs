using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection
{
    public sealed class RewardCollectionViewModel : RewardCollectionViewModelBase
    {
        private readonly Func<RewardClaimRequest, bool> _claimRequested;
        private readonly Action _closed;

        public RewardCollectionViewModel(
            RewardCollectionModelBase model,
            Func<RewardClaimRequest, bool> claimRequested,
            Action closed) : base(model)
        {
            _claimRequested = claimRequested ?? throw new ArgumentNullException(nameof(claimRequested));
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            ReceiveCommand = new RelayCommand<RewardClaimIdentity>(Receive);
            CloseCommand = new RelayCommand<object>(_ => Close());
            ReceiveCommand.AddTo(compositeDisposable);
            CloseCommand.AddTo(compositeDisposable);
        }

        public override RewardClaimPointSnapshot Point => model.Point;
        public override GuildTextSnapshot Header => model.Header;
        public override GuildTextSnapshot CloseText => model.CloseText;
        public override IReadOnlyList<RewardCollectionEntrySnapshot> Entries => model.Entries;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IReadOnlyReactiveProperty<int> Revision => model.Revision;
        public override IRelayCommand<RewardClaimIdentity> ReceiveCommand { get; }
        public override IRelayCommand<object> CloseCommand { get; }
        public override void Open() => model.Show();
        public override void Close()
        {
            if (model.IsVisible.Value) _closed();
        }

        private void Receive(RewardClaimIdentity identity)
        {
            if (identity == null) return;
            var request = new RewardClaimRequest(identity, model.Point);
            if (_claimRequested(request)) model.Remove(identity);
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

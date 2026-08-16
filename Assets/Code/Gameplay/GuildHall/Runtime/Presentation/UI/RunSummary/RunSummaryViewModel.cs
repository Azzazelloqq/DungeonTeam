using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary
{
    public sealed class RunSummaryViewModel : RunSummaryViewModelBase
    {
        private readonly Action _closed;

        public RunSummaryViewModel(RunSummaryModelBase model, Action closed) : base(model)
        {
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            CloseCommand = new RelayCommand<object>(_ => Close());
            CloseCommand.AddTo(compositeDisposable);
        }

        public override GuildRunSummarySnapshot Summary => model.Summary;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IRelayCommand<object> CloseCommand { get; }
        public override void Open() => model.Show();
        public override void Close()
        {
            if (model.IsVisible.Value)
            {
                _closed();
            }
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

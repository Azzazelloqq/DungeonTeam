using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary
{
    public sealed class RunSummaryModel : RunSummaryModelBase
    {
        private readonly ReactiveProperty<bool> _isVisible = new(false);

        public RunSummaryModel(GuildRunSummarySnapshot summary)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            _isVisible.AddTo(compositeDisposable);
        }

        public override GuildRunSummarySnapshot Summary { get; }
        public override IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public override void Show() => _isVisible.SetValue(true);
        public override void Hide() => _isVisible.SetValue(false);
        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;

namespace DungeonTeam.Gameplay.ContextActions.Runtime
{
    public sealed class ContextActionsViewModel : ContextActionsViewModelBase
    {
        public ContextActionsViewModel(ContextActionsModel model) : base(model)
        {
            ExecuteCommand = new RelayCommand<int>(model.Execute);
            ExecuteCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<IReadOnlyList<string>> Labels => model.Labels;

        public override IRelayCommand<int> ExecuteCommand { get; }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}

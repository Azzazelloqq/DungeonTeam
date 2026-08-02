using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.ContextActions.Runtime.Base
{
    public abstract class ContextActionsViewModelBase :
        ViewModelBase<ContextActionsModelBase>
    {
        protected ContextActionsViewModelBase(ContextActionsModelBase model) : base(model)
        {
        }

        public abstract IReadOnlyReactiveProperty<IReadOnlyList<string>> Labels { get; }

        public abstract IRelayCommand<int> ExecuteCommand { get; }
    }
}

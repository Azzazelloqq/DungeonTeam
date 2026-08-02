using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.ContextActions.Runtime.Base
{
    public abstract class ContextActionsModelBase : ModelBase
    {
        public abstract IReadOnlyReactiveProperty<IReadOnlyList<string>> Labels { get; }

        public abstract void SetActions(IReadOnlyList<ContextAction> actions);

        public abstract void Execute(int index);
    }
}

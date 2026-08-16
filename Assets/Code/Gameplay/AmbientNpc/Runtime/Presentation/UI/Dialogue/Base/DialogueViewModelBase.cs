using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base
{
    public abstract class DialogueViewModelBase : ViewModelBase<DialogueModelBase>
    {
        protected DialogueViewModelBase(DialogueModelBase model) : base(model) { }

        public abstract IReadOnlyReactiveProperty<string> Speaker { get; }
        public abstract IReadOnlyReactiveProperty<string> Line { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IRelayCommand<object> CloseCommand { get; }
    }
}

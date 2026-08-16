using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base
{
    public abstract class DialogueModelBase : ModelBase
    {
        public abstract IReadOnlyReactiveProperty<string> Speaker { get; }
        public abstract IReadOnlyReactiveProperty<string> Line { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }

        public abstract void Show(string speaker, string line);
        public abstract void Hide();
    }
}

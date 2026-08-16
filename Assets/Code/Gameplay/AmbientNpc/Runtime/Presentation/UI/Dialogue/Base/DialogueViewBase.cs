using Azzazelloqq.MVVM.Core;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base
{
    public abstract class DialogueViewBase : ViewMonoBehavior<DialogueViewModelBase>
    {
        public abstract void ValidateBindings();
    }
}

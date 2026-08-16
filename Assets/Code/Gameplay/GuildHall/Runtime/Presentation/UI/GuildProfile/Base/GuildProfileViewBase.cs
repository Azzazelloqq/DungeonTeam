using Azzazelloqq.MVVM.Core;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base
{
    public abstract class GuildProfileViewBase : ViewMonoBehavior<GuildProfileViewModelBase>
    {
        public abstract void ValidateBindings();
    }
}

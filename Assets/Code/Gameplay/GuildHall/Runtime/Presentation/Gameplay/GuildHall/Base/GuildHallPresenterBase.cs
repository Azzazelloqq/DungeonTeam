using MVP;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base
{
    public abstract class GuildHallPresenterBase :
        Presenter<GuildHallViewBase, GuildHallModelBase>
    {
        protected GuildHallPresenterBase(GuildHallViewBase view, GuildHallModelBase model)
            : base(view, model)
        {
        }

        public abstract void SetWorldInputBlocked(bool isBlocked);
    }
}

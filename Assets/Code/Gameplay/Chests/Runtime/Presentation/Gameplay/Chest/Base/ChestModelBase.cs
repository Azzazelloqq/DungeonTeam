using MVP;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base
{
    public abstract class ChestModelBase : Model
    {
        public abstract bool IsOpened { get; }

        public abstract bool TryOpen();
    }
}

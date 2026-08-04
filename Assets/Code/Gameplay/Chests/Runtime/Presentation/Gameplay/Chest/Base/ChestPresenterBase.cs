using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base
{
    public abstract class ChestPresenterBase :
        Presenter<ChestViewBase, ChestModelBase>
    {
        protected ChestPresenterBase(ChestViewBase view, ChestModelBase model)
            : base(view, model)
        {
        }

        public abstract Vector3 Position { get; }

        public abstract Vector3 RewardPosition { get; }

        public abstract bool IsOpened { get; }

        public abstract bool TryOpen();
    }
}

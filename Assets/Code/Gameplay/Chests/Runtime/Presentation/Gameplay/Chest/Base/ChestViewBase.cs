using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base
{
    public abstract class ChestViewBase : ViewMonoBehaviour<ChestPresenterBase>
    {
        public abstract Vector3 Position { get; }

        public abstract Vector3 RewardPosition { get; }

        public abstract void SetOpened(bool isOpened);
    }
}

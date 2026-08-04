using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base
{
    public abstract class RewardPickupViewBase :
        ViewMonoBehaviour<RewardPickupPresenterBase>
    {
        public abstract Vector3 Position { get; }

        public abstract void SetCollected(bool isCollected);
    }
}

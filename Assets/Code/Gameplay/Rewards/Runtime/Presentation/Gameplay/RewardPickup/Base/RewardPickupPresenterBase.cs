using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base
{
    public abstract class RewardPickupPresenterBase :
        Presenter<RewardPickupViewBase, RewardPickupModelBase>
    {
        protected RewardPickupPresenterBase(
            RewardPickupViewBase view,
            RewardPickupModelBase model)
            : base(view, model)
        {
        }

        public abstract Vector3 Position { get; }

        public abstract bool IsCollected { get; }

        public abstract bool TryCollect(out RewardGrant reward);
    }
}

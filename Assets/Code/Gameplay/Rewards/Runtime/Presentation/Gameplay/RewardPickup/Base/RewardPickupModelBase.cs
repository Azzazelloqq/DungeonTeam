using MVP;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base
{
    public abstract class RewardPickupModelBase : Model
    {
        public abstract bool IsCollected { get; }

        public abstract bool TryCollect(out RewardGrant reward);
    }
}

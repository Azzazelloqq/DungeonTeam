using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    internal static class RewardPickupViewAssetCatalog
    {
        public static string ResolveAddress(string rewardId)
        {
            return rewardId switch
            {
                "reward.gold" => AddressableIds.Rewards.RewardsGoldRewardPickup,
                "reward.crystal" => AddressableIds.Rewards.RewardsCrystalRewardPickup,
                "reward.silver" => AddressableIds.Rewards.RewardsRewardPickupSilver,
                _ => throw new InvalidOperationException(
                    $"Reward Pickup view for ID '{rewardId}' is not registered.")
            };
        }
    }
}

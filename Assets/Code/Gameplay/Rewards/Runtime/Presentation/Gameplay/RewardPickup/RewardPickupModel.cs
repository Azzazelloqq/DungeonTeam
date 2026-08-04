using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup
{
    public sealed class RewardPickupModel : RewardPickupModelBase
    {
        private readonly RewardGrant _reward;
        private bool _isCollected;

        public RewardPickupModel(RewardGrant reward)
        {
            if (string.IsNullOrWhiteSpace(reward.RewardId) || reward.Amount <= 0)
            {
                throw new ArgumentException("Reward grant is invalid.", nameof(reward));
            }

            _reward = reward;
        }

        public override bool IsCollected => _isCollected;

        public override bool TryCollect(out RewardGrant reward)
        {
            if (IsCollected)
            {
                reward = default;
                return false;
            }

            _isCollected = true;
            reward = _reward;
            return true;
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}

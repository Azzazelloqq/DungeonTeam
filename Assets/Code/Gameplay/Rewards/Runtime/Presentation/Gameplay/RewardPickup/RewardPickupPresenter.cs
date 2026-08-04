using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup
{
    public sealed class RewardPickupPresenter : RewardPickupPresenterBase
    {
        public RewardPickupPresenter(
            RewardPickupViewBase view,
            RewardPickupModelBase model)
            : base(view, model)
        {
        }

        public override Vector3 Position => view.Position;

        public override bool IsCollected => model.IsCollected;

        public override bool TryCollect(out RewardGrant reward)
        {
            if (!model.TryCollect(out reward))
            {
                return false;
            }

            view.SetCollected(isCollected: true);
            return true;
        }

        protected override void OnInitialize()
        {
            view.SetCollected(model.IsCollected);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            view.SetCollected(model.IsCollected);
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

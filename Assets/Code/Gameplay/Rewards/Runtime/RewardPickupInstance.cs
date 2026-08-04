using System;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardPickupInstance : IDisposable
    {
        private RewardPickupPresenterBase _presenter;

        internal RewardPickupInstance(RewardPickupPresenterBase presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public Vector3 Position => RequirePresenter().Position;

        public bool IsCollected => RequirePresenter().IsCollected;

        public bool TryCollect(out RewardGrant reward)
        {
            return RequirePresenter().TryCollect(out reward);
        }

        public void Dispose()
        {
            var presenter = _presenter;
            _presenter = null;
            presenter?.Dispose();
        }

        private RewardPickupPresenterBase RequirePresenter()
        {
            return _presenter ?? throw new ObjectDisposedException(nameof(RewardPickupInstance));
        }
    }
}

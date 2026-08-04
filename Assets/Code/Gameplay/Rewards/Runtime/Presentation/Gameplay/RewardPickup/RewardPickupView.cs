using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup
{
    public sealed class RewardPickupView : RewardPickupViewBase
    {
        [SerializeField]
        private GameObject _visualRoot;

        public override Vector3 Position => transform.position;

        public override void SetCollected(bool isCollected)
        {
            _visualRoot.SetActive(!isCollected);
        }

        protected override void OnInitialize()
        {
            ValidateBindings();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            ValidateBindings();
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        private void ValidateBindings()
        {
            if (_visualRoot == null)
            {
                throw new InvalidOperationException(
                    "Reward Pickup View requires a visual root binding.");
            }
        }
    }
}

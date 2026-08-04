using System;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public sealed class ChestInstance : IDisposable
    {
        private ChestPresenterBase _presenter;

        internal ChestInstance(
            ChestPresenterBase presenter,
            string rewardProfileId)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            RewardProfileId = !string.IsNullOrWhiteSpace(rewardProfileId)
                ? rewardProfileId
                : throw new ArgumentException(
                    "Chest reward profile id cannot be empty.",
                    nameof(rewardProfileId));
        }

        public Vector3 Position => RequirePresenter().Position;

        public Vector3 RewardPosition => RequirePresenter().RewardPosition;

        public bool IsOpened => RequirePresenter().IsOpened;

        public string RewardProfileId { get; }

        public bool TryOpen()
        {
            return RequirePresenter().TryOpen();
        }

        public void Dispose()
        {
            var presenter = _presenter;
            _presenter = null;
            presenter?.Dispose();
        }

        private ChestPresenterBase RequirePresenter()
        {
            return _presenter ?? throw new ObjectDisposedException(nameof(ChestInstance));
        }
    }
}

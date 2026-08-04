using System;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup;
using DungeonTeam.Gameplay.Rewards.Runtime.Presentation.Gameplay.RewardPickup.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardPickupFactory
    {
        public RewardPickupInstance Create(
            RewardPickupViewBase prefab,
            RewardPickupSpawnRequest request,
            Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            RewardPickupViewBase view = null;
            RewardPickupModelBase model = null;
            RewardPickupPresenterBase presenter = null;
            try
            {
                view = UnityEngine.Object.Instantiate(
                    prefab,
                    request.Position,
                    Quaternion.identity,
                    parent);
                view.name = $"RewardPickup_{request.Definition.RewardId}";

                model = new RewardPickupModel(request.Grant);
                presenter = new RewardPickupPresenter(view, model);
                presenter.Initialize();
                return new RewardPickupInstance(presenter);
            }
            catch
            {
                if (presenter != null)
                {
                    presenter.Dispose();
                }
                else
                {
                    model?.Dispose();
                    view?.Dispose();
                }

                throw;
            }
        }
    }
}

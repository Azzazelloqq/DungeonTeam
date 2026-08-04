using System;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public sealed class ChestFactory
    {
        public ChestInstance Create(
            ChestViewBase prefab,
            ChestSpawnRequest request,
            Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            ChestViewBase view = null;
            ChestModelBase model = null;
            ChestPresenterBase presenter = null;
            try
            {
                view = UnityEngine.Object.Instantiate(
                    prefab,
                    request.Position,
                    request.Rotation,
                    parent);
                view.name = request.InstanceName;

                model = new ChestModel();
                presenter = new ChestPresenter(view, model);
                presenter.Initialize();
                return new ChestInstance(presenter, request.RewardProfileId);
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

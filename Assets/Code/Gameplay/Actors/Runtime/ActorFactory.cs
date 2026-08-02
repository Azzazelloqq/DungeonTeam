using System;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorFactory
    {
        public ActorInstance Create(
            ActorViewBase prefab,
            ActorSpawnRequest request,
            Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            ActorPresenterBase presenter = null;
            ActorViewBase view = null;
            try
            {
                view = UnityEngine.Object.Instantiate(
                    prefab,
                    request.Position,
                    request.Rotation,
                    parent);
                view.name = request.InstanceName;

                var model = new ActorModel(request.MaximumHealth);
                presenter = new ActorPresenter(
                    view,
                    model,
                    request.Color,
                    request.MovementSpeed);
                presenter.Initialize();
                return new ActorInstance(presenter, view.gameObject);
            }
            catch
            {
                presenter?.Dispose();
                if (view != null)
                {
                    Destroy(view.gameObject);
                }

                throw;
            }
        }

        private static void Destroy(GameObject instance)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}

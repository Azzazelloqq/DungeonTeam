using System;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorFactory
    {
        public ActorInstance Create(
            ActorDefinition definition,
            ActorSpawnRequest request,
            Transform parent = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ActorPresenterBase presenter = null;
            ActorViewBase view = null;
            try
            {
                view = UnityEngine.Object.Instantiate(
                    definition.Prefab,
                    request.Position,
                    request.Rotation,
                    parent);
                view.name = request.InstanceName;

                var model = new ActorModel(definition.MaximumHealth);
                presenter = new ActorPresenter(
                    view,
                    model,
                    definition.MovementSpeed);
                presenter.Initialize();
                return new ActorInstance(
                    definition.ActorId,
                    presenter,
                    view.gameObject);
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

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
            ActorRuntimeDefinition runtimeDefinition,
            ActorSpawnRequest request,
            Transform parent = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!string.Equals(
                    definition.ActorId,
                    runtimeDefinition.ActorId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Actor view ID '{definition.ActorId}' does not match runtime actor ID " +
                    $"'{runtimeDefinition.ActorId}'.",
                    nameof(runtimeDefinition));
            }

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

                var model = new ActorModel(runtimeDefinition.MaximumHealth);
                presenter = new ActorPresenter(
                    view,
                    model,
                    runtimeDefinition.MovementSpeed);
                presenter.Initialize();
                return new ActorInstance(
                    definition.ActorId,
                    runtimeDefinition.Level,
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

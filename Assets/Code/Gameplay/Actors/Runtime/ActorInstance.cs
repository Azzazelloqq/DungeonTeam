using System;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorInstance : IDisposable
    {
        private ActorPresenterBase _presenter;
        private GameObject _gameObject;

        internal ActorInstance(ActorPresenterBase presenter, GameObject gameObject)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _gameObject = gameObject != null
                ? gameObject
                : throw new ArgumentNullException(nameof(gameObject));
        }

        public Vector3 Position => RequirePresenter().Position;

        public int CurrentHealth => RequirePresenter().CurrentHealth;

        public bool IsAlive => RequirePresenter().IsAlive;

        public bool TryMoveTo(Vector3 destination)
        {
            return RequirePresenter().TryMoveTo(destination);
        }

        public bool SetMoveDirection(Vector3 direction)
        {
            return RequirePresenter().SetMoveDirection(direction);
        }

        public void StopMovement()
        {
            RequirePresenter().StopMovement();
        }

        public ActorDamageResult ApplyDamage(int amount)
        {
            return RequirePresenter().ApplyDamage(amount);
        }

        public void Dispose()
        {
            var presenter = _presenter;
            var gameObject = _gameObject;
            _presenter = null;
            _gameObject = null;
            presenter?.Dispose();

            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private ActorPresenterBase RequirePresenter()
        {
            return _presenter ?? throw new ObjectDisposedException(nameof(ActorInstance));
        }
    }
}

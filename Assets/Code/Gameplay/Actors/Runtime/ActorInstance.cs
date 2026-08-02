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

        public Vector3 Forward => RequirePresenter().Forward;

        public int CurrentHealth => RequirePresenter().CurrentHealth;

        public bool IsAlive => RequirePresenter().IsAlive;

        public event Action<ActorInstance> AttackedBy;

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

        public void PlayAttackFeedback()
        {
            RequirePresenter().PlayAttackFeedback();
        }

        public void SetTargetHighlighted(bool isHighlighted)
        {
            RequirePresenter().SetTargetHighlighted(isHighlighted);
        }

        public ActorDamageResult ApplyDamage(int amount)
        {
            return ApplyDamage(amount, attacker: null);
        }

        public ActorDamageResult ApplyDamage(int amount, ActorInstance attacker)
        {
            var result = RequirePresenter().ApplyDamage(amount);
            if (result != ActorDamageResult.Ignored && attacker != null)
            {
                AttackedBy?.Invoke(attacker);
            }

            return result;
        }

        public void Dispose()
        {
            var presenter = _presenter;
            var gameObject = _gameObject;
            _presenter = null;
            _gameObject = null;
            AttackedBy = null;
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

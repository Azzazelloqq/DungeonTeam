using System;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public enum ActorSkillAnimationCue
    {
        Attack = 0,
        Cast = 1
    }

    public sealed class ActorInstance : IDisposable
    {
        private ActorPresenterBase _presenter;
        private GameObject _gameObject;

        internal ActorInstance(
            string actorId,
            int level,
            ActorPresenterBase presenter,
            GameObject gameObject)
        {
            ActorId = !string.IsNullOrWhiteSpace(actorId)
                ? actorId
                : throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            Level = level > 0
                ? level
                : throw new ArgumentOutOfRangeException(nameof(level));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _gameObject = gameObject != null
                ? gameObject
                : throw new ArgumentNullException(nameof(gameObject));
        }

        public string ActorId { get; }

        public int Level { get; }

        public Vector3 Position => RequirePresenter().Position;

        public Vector3 Forward => RequirePresenter().Forward;

        public int CurrentHealth => RequirePresenter().CurrentHealth;

        public int MaximumHealth => RequirePresenter().MaximumHealth;

        public bool IsAlive => RequirePresenter().IsAlive;

        public Transform WeaponAnchor => RequirePresenter().WeaponAnchor;

        public Transform HitVfxAnchor => RequirePresenter().HitVfxAnchor;

        public Transform OverheadAnchor => RequirePresenter().OverheadAnchor;

        public Transform SkillOriginAnchor => RequirePresenter().SkillOriginAnchor;

        public event Action<ActorInstance> AttackedBy;

        public event Action<ActorInstance> Died;

        public bool TryMoveTo(Vector3 destination)
        {
            return RequirePresenter().TryMoveTo(destination);
        }

        public bool SetMoveDirection(Vector3 direction)
        {
            return RequirePresenter().SetMoveDirection(direction);
        }

        public bool TryFaceTowards(Vector3 targetPosition)
        {
            return RequirePresenter().TryFaceTowards(targetPosition);
        }

        public void StopMovement()
        {
            RequirePresenter().StopMovement();
        }

        public void PlayAttackFeedback()
        {
            RequirePresenter().PlayAttackFeedback();
        }

        public void PlaySkillFeedback(ActorSkillAnimationCue cue)
        {
            switch (cue)
            {
                case ActorSkillAnimationCue.Attack:
                    RequirePresenter().PlayAttackFeedback();
                    break;
                case ActorSkillAnimationCue.Cast:
                    RequirePresenter().PlayCastFeedback();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cue), cue, null);
            }
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

            if (result == ActorDamageResult.Killed)
            {
                Died?.Invoke(this);
            }

            return result;
        }

        public ActorHealResult ApplyHeal(int amount)
        {
            return RequirePresenter().ApplyHeal(amount);
        }

        public void Dispose()
        {
            var presenter = _presenter;
            var gameObject = _gameObject;
            _presenter = null;
            _gameObject = null;
            AttackedBy = null;
            Died = null;
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

using System;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public sealed class SkillProjectileInstance : IDisposable
    {
        private SkillProjectilePresenterBase _presenter;
        private GameObject _gameObject;

        internal SkillProjectileInstance(
            SkillProjectilePresenterBase presenter,
            GameObject gameObject)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _gameObject = gameObject != null
                ? gameObject
                : throw new ArgumentNullException(nameof(gameObject));
        }

        public bool IsCompleted => RequirePresenter().IsCompleted;

        public void Tick(float deltaTime)
        {
            RequirePresenter().Tick(deltaTime);
        }

        public void Dispose()
        {
            var presenter = _presenter;
            var gameObject = _gameObject;
            _presenter = null;
            _gameObject = null;
            presenter?.Dispose();
            if (gameObject == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private SkillProjectilePresenterBase RequirePresenter()
        {
            return _presenter ?? throw new ObjectDisposedException(nameof(SkillProjectileInstance));
        }
    }
}

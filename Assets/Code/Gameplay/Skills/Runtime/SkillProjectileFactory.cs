using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    internal sealed class SkillProjectileFactory
    {
        public SkillProjectileInstance Create(
            string skillId,
            ProjectileDamageSkillLevelDefinition level,
            ActorInstance source,
            ActorInstance target,
            SkillViewSet views,
            Transform parent,
            Action<Vector3> onImpact)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (onImpact == null) throw new ArgumentNullException(nameof(onImpact));
            if (source.SkillOriginAnchor == null)
            {
                throw new InvalidOperationException(
                    $"Actor '{source.ActorId}' has no Skill Origin Anchor.");
            }

            SkillProjectileViewBase view = null;
            SkillProjectilePresenterBase presenter = null;
            try
            {
                var prefab = views.RequireProjectile(skillId);
                view = UnityEngine.Object.Instantiate(
                    prefab,
                    source.SkillOriginAnchor.position,
                    source.SkillOriginAnchor.rotation * prefab.transform.localRotation,
                    parent);
                view.name = $"SkillProjectile_{skillId}";
                var model = new SkillProjectileModel(level.Damage, level.ProjectileSpeed);
                presenter = new SkillProjectilePresenter(
                    view,
                    model,
                    source,
                    target,
                    onImpact);
                presenter.Initialize();
                return new SkillProjectileInstance(presenter, view.gameObject);
            }
            catch
            {
                presenter?.Dispose();
                if (view != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(view.gameObject);
                    else
                        UnityEngine.Object.DestroyImmediate(view.gameObject);
                }

                throw;
            }
        }
    }
}

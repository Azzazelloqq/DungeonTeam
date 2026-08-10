using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public readonly struct SkillProjectileViewEntry
    {
        public SkillProjectileViewEntry(string skillId, SkillProjectileViewBase prefab)
        {
            SkillId = !string.IsNullOrWhiteSpace(skillId)
                ? skillId
                : throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));
            Prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public string SkillId { get; }
        public SkillProjectileViewBase Prefab { get; }
    }

    public readonly struct SkillPresentationViewEntry
    {
        public SkillPresentationViewEntry(
            string skillId,
            SkillPresentationSequence sequence)
        {
            SkillId = !string.IsNullOrWhiteSpace(skillId)
                ? skillId
                : throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
        }

        public string SkillId { get; }
        public SkillPresentationSequence Sequence { get; }
    }

    public sealed class SkillViewSet : IDisposable
    {
        private Dictionary<string, SkillProjectileViewBase> _projectiles;
        private Dictionary<string, SkillPresentationSequence> _presentations;
        private IResourceLoader _resourceLoader;
        private UnityEngine.Object[] _loadedAssets;

        public SkillViewSet(SkillProjectileViewEntry[] projectiles)
            : this(projectiles, Array.Empty<SkillPresentationViewEntry>())
        {
        }

        public SkillViewSet(
            SkillProjectileViewEntry[] projectiles,
            SkillPresentationViewEntry[] presentations)
            : this(projectiles, presentations, null, null)
        {
        }

        internal SkillViewSet(
            SkillProjectileViewEntry[] projectiles,
            SkillPresentationViewEntry[] presentations,
            IResourceLoader resourceLoader,
            UnityEngine.Object[] loadedAssets)
        {
            if (projectiles == null)
                throw new ArgumentNullException(nameof(projectiles));
            if (presentations == null)
                throw new ArgumentNullException(nameof(presentations));
            if ((resourceLoader == null) != (loadedAssets == null))
            {
                throw new ArgumentException(
                    "Resource loader and loaded assets must be provided together.");
            }

            _projectiles = new Dictionary<string, SkillProjectileViewBase>(
                projectiles.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < projectiles.Length; index++)
            {
                var entry = projectiles[index];
                if (!_projectiles.TryAdd(entry.SkillId, entry.Prefab))
                {
                    throw new ArgumentException(
                        $"Projectile view for skill ID '{entry.SkillId}' was loaded more than once.",
                    nameof(projectiles));
                }
            }

            _presentations = new Dictionary<string, SkillPresentationSequence>(
                presentations.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < presentations.Length; index++)
            {
                var entry = presentations[index];
                if (!_presentations.TryAdd(entry.SkillId, entry.Sequence))
                {
                    throw new ArgumentException(
                        $"Presentation for skill ID '{entry.SkillId}' was loaded more than once.",
                        nameof(presentations));
                }
            }

            _resourceLoader = resourceLoader;
            _loadedAssets = loadedAssets;
        }

        public bool IsDisposed => _projectiles == null;

        public SkillProjectileViewBase RequireProjectile(string skillId)
        {
            var projectiles = _projectiles ?? throw new ObjectDisposedException(nameof(SkillViewSet));
            if (!projectiles.TryGetValue(skillId, out var prefab))
            {
                throw new InvalidOperationException(
                    $"Loaded skill views do not contain projectile skill ID '{skillId}'.");
            }

            return prefab;
        }

        public SkillPresentationSequence RequirePresentation(string skillId)
        {
            var presentations = _presentations ??
                                throw new ObjectDisposedException(nameof(SkillViewSet));
            if (!presentations.TryGetValue(skillId, out var sequence))
            {
                throw new InvalidOperationException(
                    $"Loaded skill views do not contain presentation for skill ID '{skillId}'.");
            }

            return sequence;
        }

        public void Dispose()
        {
            var loadedAssets = _loadedAssets;
            var resourceLoader = _resourceLoader;
            _projectiles = null;
            _presentations = null;
            _loadedAssets = null;
            _resourceLoader = null;
            if (loadedAssets == null)
                return;

            for (var index = loadedAssets.Length - 1; index >= 0; index--)
            {
                resourceLoader.ReleaseResource(loadedAssets[index]);
            }
        }
    }
}

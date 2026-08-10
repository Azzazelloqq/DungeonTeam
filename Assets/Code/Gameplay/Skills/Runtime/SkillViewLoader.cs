using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public sealed class SkillViewLoader : ISkillViewLoader
    {
        private readonly SkillCatalog _catalog;
        private readonly IResourceLoader _resourceLoader;

        public SkillViewLoader(SkillCatalog catalog, IResourceLoader resourceLoader)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
        }

        public async UniTask<SkillViewSet> LoadAsync(
            IReadOnlyList<string> loadoutIds,
            CancellationToken token)
        {
            if (loadoutIds == null)
                throw new ArgumentNullException(nameof(loadoutIds));

            var skillIds = GetSkillIds(loadoutIds);
            var projectileSkillIds = GetProjectileSkillIds(skillIds);
            var entries = new SkillProjectileViewEntry[projectileSkillIds.Count];
            var presentations = new SkillPresentationViewEntry[skillIds.Count];
            var loadedAssets = new UnityEngine.Object[
                projectileSkillIds.Count + skillIds.Count];
            var loadedCount = 0;
            try
            {
                for (var index = 0; index < projectileSkillIds.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var skillId = projectileSkillIds[index];
                    var prefabObject = await _resourceLoader.LoadResourceAsync<GameObject>(
                        SkillViewAssetCatalog.ResolveProjectileAddress(skillId),
                        token);
                    if (prefabObject != null)
                    {
                        loadedAssets[loadedCount++] = prefabObject;
                    }

                    token.ThrowIfCancellationRequested();
                    if (prefabObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Projectile prefab for skill ID '{skillId}' could not be loaded.");
                    }

                    var prefab = prefabObject.GetComponent<SkillProjectileViewBase>();
                    if (prefab == null)
                    {
                        throw new InvalidOperationException(
                            $"Projectile prefab for skill ID '{skillId}' requires a " +
                            $"{nameof(SkillProjectileViewBase)} component on its root.");
                    }

                    entries[index] = new SkillProjectileViewEntry(skillId, prefab);
                }

                var sequencesByAddress = new Dictionary<string, SkillPresentationSequence>(
                    StringComparer.Ordinal);
                for (var index = 0; index < skillIds.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var skillId = skillIds[index];
                    var address = SkillViewAssetCatalog.ResolvePresentationAddress(skillId);
                    if (!sequencesByAddress.TryGetValue(address, out var sequence))
                    {
                        var asset = await _resourceLoader.LoadResourceAsync<SkillPresentationAsset>(
                            address,
                            token);
                        if (asset != null)
                        {
                            loadedAssets[loadedCount++] = asset;
                        }

                        token.ThrowIfCancellationRequested();
                        if (asset == null)
                        {
                            throw new InvalidOperationException(
                                $"Presentation asset for skill ID '{skillId}' could not be loaded.");
                        }

                        try
                        {
                            sequence = asset.CreateSequence();
                        }
                        catch (Exception exception)
                        {
                            throw new InvalidOperationException(
                                $"Presentation asset '{asset.name}' for skill ID '{skillId}' " +
                                "is invalid.",
                                exception);
                        }

                        sequencesByAddress.Add(address, sequence);
                    }

                    presentations[index] = new SkillPresentationViewEntry(skillId, sequence);
                }

                Array.Resize(ref loadedAssets, loadedCount);
                return new SkillViewSet(
                    entries,
                    presentations,
                    _resourceLoader,
                    loadedAssets);
            }
            catch
            {
                for (var index = loadedCount - 1; index >= 0; index--)
                {
                    _resourceLoader.ReleaseResource(loadedAssets[index]);
                }

                throw;
            }
        }

        private List<string> GetSkillIds(IReadOnlyList<string> loadoutIds)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var loadoutIndex = 0; loadoutIndex < loadoutIds.Count; loadoutIndex++)
            {
                var loadoutId = loadoutIds[loadoutIndex];
                var loadout = _catalog.RequireLoadout(loadoutId);
                for (var slotIndex = 0; slotIndex < loadout.Slots.Count; slotIndex++)
                {
                    var skillId = loadout.Slots[slotIndex].SkillId;
                    _catalog.RequireSkill(skillId);
                    if (seen.Add(skillId))
                    {
                        result.Add(skillId);
                    }
                }
            }

            return result;
        }

        private List<string> GetProjectileSkillIds(IReadOnlyList<string> skillIds)
        {
            var result = new List<string>();
            for (var index = 0; index < skillIds.Count; index++)
            {
                var skillId = skillIds[index];
                if (_catalog.RequireSkill(skillId) is ProjectileDamageSkillDefinition)
                {
                    result.Add(skillId);
                }
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Editor
{
    internal sealed class SkillVfxLabCatalog
    {
        internal const string SkillConfigPath =
            "Assets/Content/Configuration/SkillConfig.asset";
        internal const string ActorConfigPath =
            "Assets/Content/Configuration/ActorConfig.asset";

        private readonly SkillConfigPage _skillConfig;

        private SkillVfxLabCatalog(
            SkillConfigPage skillConfig,
            IReadOnlyList<SkillVfxLabSkill> skills,
            IReadOnlyList<SkillVfxLabActor> actors)
        {
            _skillConfig = skillConfig;
            Skills = skills;
            Actors = actors;
        }

        public IReadOnlyList<SkillVfxLabSkill> Skills { get; }
        public IReadOnlyList<SkillVfxLabActor> Actors { get; }

        public static SkillVfxLabCatalog Load()
        {
            var skillConfig = LoadRequired<SkillConfigPage>(SkillConfigPath);
            var actorConfig = LoadRequired<ActorConfigPage>(ActorConfigPath);
            var skillCatalog = skillConfig.CreateCatalog();
            var actorCatalog = actorConfig.CreateCatalog();

            var skills = new SkillVfxLabSkill[skillCatalog.Skills.Count];
            for (var index = 0; index < skills.Length; index++)
            {
                var definition = skillCatalog.Skills[index];
                var view = SkillViewAssetCatalog.Resolve(definition.SkillId);
                skills[index] = new SkillVfxLabSkill(
                    definition,
                    LoadAddressable<SkillPresentationAsset>(view.PresentationAddress),
                    string.IsNullOrWhiteSpace(view.ProjectileAddress)
                        ? null
                        : LoadAddressable<GameObject>(view.ProjectileAddress));
            }

            var actors = new SkillVfxLabActor[actorCatalog.Definitions.Count];
            for (var index = 0; index < actors.Length; index++)
            {
                var definition = actorCatalog.Definitions[index];
                actors[index] = new SkillVfxLabActor(
                    definition.ActorId,
                    definition.DisplayName,
                    LoadAddressable<GameObject>(
                        ActorViewAssetCatalog.ResolveAddress(definition.ActorId)));
            }

            return new SkillVfxLabCatalog(skillConfig, skills, actors);
        }

        public void ApplyLevelTiming(
            string skillId,
            int level,
            float commitDelay,
            float recoveryDuration,
            float? projectileSpeed)
        {
            ApplyLevelTiming(
                _skillConfig,
                skillId,
                level,
                commitDelay,
                recoveryDuration,
                projectileSpeed);
        }

        internal static void ApplyLevelTiming(
            SkillConfigPage skillConfig,
            string skillId,
            int level,
            float commitDelay,
            float recoveryDuration,
            float? projectileSpeed)
        {
            if (skillConfig == null)
                throw new ArgumentNullException(nameof(skillConfig));
            if (!IsFinite(commitDelay) || commitDelay < 0f ||
                !IsFinite(recoveryDuration) || recoveryDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(commitDelay));
            if (projectileSpeed.HasValue &&
                (!IsFinite(projectileSpeed.Value) || projectileSpeed.Value <= 0f))
                throw new ArgumentOutOfRangeException(nameof(projectileSpeed));

            var serialized = new SerializedObject(skillConfig);
            var levelProperty = FindLevel(serialized, skillId, level) ??
                                throw new InvalidOperationException(
                                    $"Skill '{skillId}' level {level} is missing from SkillConfig.");

            Undo.RecordObject(skillConfig, "Apply skill timing from VFX Lab");
            levelProperty.FindPropertyRelative("_commitDelay").floatValue = commitDelay;
            levelProperty.FindPropertyRelative("_recoveryDuration").floatValue = recoveryDuration;
            if (projectileSpeed.HasValue)
            {
                var speed = levelProperty.FindPropertyRelative("_projectileSpeed") ??
                            throw new InvalidOperationException(
                                $"Skill '{skillId}' level {level} has no projectile speed.");
                speed.floatValue = projectileSpeed.Value;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(skillConfig);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static SerializedProperty FindLevel(
            SerializedObject config,
            string skillId,
            int level)
        {
            var skillArrays = new[]
            {
                "_directDamageSkills",
                "_projectileDamageSkills",
                "_directHealSkills"
            };

            for (var arrayIndex = 0; arrayIndex < skillArrays.Length; arrayIndex++)
            {
                var skills = config.FindProperty(skillArrays[arrayIndex]);
                for (var skillIndex = 0; skillIndex < skills.arraySize; skillIndex++)
                {
                    var skill = skills.GetArrayElementAtIndex(skillIndex);
                    if (skill.FindPropertyRelative("_skillId").stringValue != skillId)
                        continue;

                    var levels = skill.FindPropertyRelative("_levels");
                    for (var levelIndex = 0; levelIndex < levels.arraySize; levelIndex++)
                    {
                        var candidate = levels.GetArrayElementAtIndex(levelIndex);
                        if (candidate.FindPropertyRelative("_level").intValue == level)
                            return candidate;
                    }

                    return null;
                }
            }

            return null;
        }

        private static T LoadAddressable<T>(string address) where T : UnityEngine.Object
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false) ??
                           throw new InvalidOperationException(
                               "AddressableAssetSettings is not configured.");

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (var entry in group.entries)
                {
                    if (!string.Equals(entry.address, address, StringComparison.Ordinal))
                        continue;

                    var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                    return LoadRequired<T>(path);
                }
            }

            throw new InvalidOperationException(
                $"Addressable asset '{address}' is not registered.");
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(
                       $"Required asset of type '{typeof(T).Name}' is missing at '{path}'.");
        }
    }

    internal readonly struct SkillVfxLabSkill
    {
        public SkillVfxLabSkill(
            SkillDefinition definition,
            SkillPresentationAsset presentation,
            GameObject projectilePrefab)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Presentation = presentation != null
                ? presentation
                : throw new ArgumentNullException(nameof(presentation));
            ProjectilePrefab = projectilePrefab;
        }

        public SkillDefinition Definition { get; }
        public SkillPresentationAsset Presentation { get; }
        public GameObject ProjectilePrefab { get; }
        public string Label => $"{Definition.DisplayName}  [{Definition.SkillId}]";
    }

    internal readonly struct SkillVfxLabActor
    {
        public SkillVfxLabActor(string actorId, string displayName, GameObject prefab)
        {
            ActorId = actorId;
            DisplayName = displayName;
            Prefab = prefab;
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public GameObject Prefab { get; }
        public string Label => $"{DisplayName}  [{ActorId}]";
    }
}

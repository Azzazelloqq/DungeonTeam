using System.Collections.Generic;
using System.Linq;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Editor;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class SkillVfxLabTests
    {
        [Test]
        public void Load_ResolvesEveryProductionSkillAndActorAsset()
        {
            var catalog = SkillVfxLabCatalog.Load();

            Assert.That(catalog.Skills.Count, Is.EqualTo(10));
            Assert.That(catalog.Actors.Count, Is.EqualTo(6));
            Assert.That(catalog.Skills.Select(skill => skill.Definition.SkillId), Is.Unique);
            Assert.That(catalog.Actors.Select(actor => actor.ActorId), Is.Unique);
            Assert.That(catalog.Skills.All(skill => skill.Presentation != null), Is.True);
            Assert.That(catalog.Actors.All(actor => actor.Prefab != null), Is.True);

            foreach (var skill in catalog.Skills)
            {
                if (skill.Definition is ProjectileDamageSkillDefinition)
                    Assert.That(skill.ProjectilePrefab, Is.Not.Null, skill.Label);
                else
                    Assert.That(skill.ProjectilePrefab, Is.Null, skill.Label);
            }
        }

        [Test]
        public void ApplyLevelTiming_UpdatesOnlyRequestedDraftLevel()
        {
            var production = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(
                SkillVfxLabCatalog.SkillConfigPath);
            var draft = Object.Instantiate(production);
            draft.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                SkillVfxLabCatalog.ApplyLevelTiming(
                    draft,
                    "skill.fireball",
                    2,
                    0.91f,
                    0.47f,
                    12.5f);

                var catalog = draft.CreateCatalog();
                var changed = (ProjectileDamageSkillLevelDefinition)catalog
                    .RequireSkill("skill.fireball")
                    .RequireLevel(2);
                var untouched = (ProjectileDamageSkillLevelDefinition)catalog
                    .RequireSkill("skill.fireball")
                    .RequireLevel(1);

                Assert.That(changed.UseTiming.CommitDelay, Is.EqualTo(0.91f));
                Assert.That(changed.UseTiming.RecoveryDuration, Is.EqualTo(0.47f));
                Assert.That(changed.ProjectileSpeed, Is.EqualTo(12.5f));
                Assert.That(untouched.UseTiming.CommitDelay, Is.EqualTo(0.35f));
                Assert.That(untouched.ProjectileSpeed, Is.EqualTo(8f));
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void ProductionVfxCues_UseHierarchyParticleScaling()
        {
            var catalog = SkillVfxLabCatalog.Load();
            var prefabs = catalog.Skills
                .SelectMany(skill => skill.Presentation.CreateSequence().VfxCues)
                .Select(cue => cue.Prefab)
                .Distinct();

            foreach (var prefab in prefabs)
            {
                foreach (var particleSystem in
                         prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(
                        particleSystem.main.scalingMode,
                        Is.EqualTo(ParticleSystemScalingMode.Hierarchy),
                        $"{prefab.name}/{particleSystem.name} must follow Scale Multiplier.");
                }
            }
        }

        [Test]
        public void ProductionProjectilePrefabs_UseFireballParticleLayoutAndLogic()
        {
            var projectilePrefabs = SkillVfxLabCatalog.Load().Skills
                .Select(skill => skill.ProjectilePrefab)
                .Where(prefab => prefab != null)
                .Distinct()
                .ToArray();
            var fireball = projectilePrefabs.Single(prefab =>
                prefab.name == "FireballProjectile");
            var expectedSystems = GetParticleSystemsByPath(fireball);

            foreach (var prefab in projectilePrefabs)
            {
                Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero), prefab.name);

                var actualSystems = GetParticleSystemsByPath(prefab);
                Assert.That(
                    actualSystems.Keys,
                    Is.EquivalentTo(expectedSystems.Keys),
                    $"{prefab.name} must use the Fireball particle hierarchy.");

                foreach (var pair in expectedSystems)
                    AssertParticleLogicMatches(pair.Value, actualSystems[pair.Key], prefab.name, pair.Key);
            }
        }

        private static Dictionary<string, ParticleSystem> GetParticleSystemsByPath(
            GameObject prefab)
        {
            return prefab.GetComponentsInChildren<ParticleSystem>(true).ToDictionary(
                system => system.transform == prefab.transform
                    ? "."
                    : AnimationUtility.CalculateTransformPath(system.transform, prefab.transform));
        }

        private static void AssertParticleLogicMatches(
            ParticleSystem expected,
            ParticleSystem actual,
            string prefabName,
            string path)
        {
            var context = $"{prefabName}/{path}";
            var expectedMain = expected.main;
            var actualMain = actual.main;
            Assert.That(actualMain.duration, Is.EqualTo(expectedMain.duration), context);
            Assert.That(actualMain.loop, Is.EqualTo(expectedMain.loop), context);
            Assert.That(actualMain.playOnAwake, Is.EqualTo(expectedMain.playOnAwake), context);
            Assert.That(actualMain.simulationSpace, Is.EqualTo(expectedMain.simulationSpace), context);
            Assert.That(actualMain.scalingMode, Is.EqualTo(expectedMain.scalingMode), context);
            Assert.That(actualMain.maxParticles, Is.EqualTo(expectedMain.maxParticles), context);
            AssertCurveMatches(expectedMain.startDelay, actualMain.startDelay, context);
            AssertCurveMatches(expectedMain.startLifetime, actualMain.startLifetime, context);
            AssertCurveMatches(expectedMain.startSpeed, actualMain.startSpeed, context);
            AssertCurveMatches(expectedMain.startSize, actualMain.startSize, context);
            AssertCurveMatches(expectedMain.startRotation, actualMain.startRotation, context);

            var expectedEmission = expected.emission;
            var actualEmission = actual.emission;
            Assert.That(actualEmission.enabled, Is.EqualTo(expectedEmission.enabled), context);
            AssertCurveMatches(expectedEmission.rateOverTime, actualEmission.rateOverTime, context);
            AssertCurveMatches(expectedEmission.rateOverDistance, actualEmission.rateOverDistance, context);
            Assert.That(actualEmission.burstCount, Is.EqualTo(expectedEmission.burstCount), context);

            var expectedShape = expected.shape;
            var actualShape = actual.shape;
            Assert.That(actualShape.enabled, Is.EqualTo(expectedShape.enabled), context);
            Assert.That(actualShape.shapeType, Is.EqualTo(expectedShape.shapeType), context);
            Assert.That(actualShape.angle, Is.EqualTo(expectedShape.angle), context);
            Assert.That(actualShape.radius, Is.EqualTo(expectedShape.radius), context);
            Assert.That(actualShape.radiusThickness, Is.EqualTo(expectedShape.radiusThickness), context);

            Assert.That(actual.colorOverLifetime.enabled,
                Is.EqualTo(expected.colorOverLifetime.enabled), context);
            Assert.That(actual.velocityOverLifetime.enabled,
                Is.EqualTo(expected.velocityOverLifetime.enabled), context);
            Assert.That(actual.limitVelocityOverLifetime.enabled,
                Is.EqualTo(expected.limitVelocityOverLifetime.enabled), context);
            Assert.That(actual.inheritVelocity.enabled,
                Is.EqualTo(expected.inheritVelocity.enabled), context);
            Assert.That(actual.forceOverLifetime.enabled,
                Is.EqualTo(expected.forceOverLifetime.enabled), context);
            Assert.That(actual.sizeOverLifetime.enabled,
                Is.EqualTo(expected.sizeOverLifetime.enabled), context);
            Assert.That(actual.rotationOverLifetime.enabled,
                Is.EqualTo(expected.rotationOverLifetime.enabled), context);
            Assert.That(actual.noise.enabled, Is.EqualTo(expected.noise.enabled), context);
            Assert.That(actual.textureSheetAnimation.enabled,
                Is.EqualTo(expected.textureSheetAnimation.enabled), context);
            Assert.That(actual.trails.enabled, Is.EqualTo(expected.trails.enabled), context);

            var expectedRenderer = expected.GetComponent<ParticleSystemRenderer>();
            var actualRenderer = actual.GetComponent<ParticleSystemRenderer>();
            Assert.That(actualRenderer.renderMode, Is.EqualTo(expectedRenderer.renderMode), context);
            Assert.That(actualRenderer.alignment, Is.EqualTo(expectedRenderer.alignment), context);
            Assert.That(
                AssetDatabase.GetAssetPath(actualRenderer.sharedMaterial),
                Is.EqualTo(AssetDatabase.GetAssetPath(expectedRenderer.sharedMaterial)),
                context);
        }

        private static void AssertCurveMatches(
            ParticleSystem.MinMaxCurve expected,
            ParticleSystem.MinMaxCurve actual,
            string context)
        {
            Assert.That(actual.mode, Is.EqualTo(expected.mode), context);
            Assert.That(actual.curveMultiplier, Is.EqualTo(expected.curveMultiplier), context);
            Assert.That(actual.constantMin, Is.EqualTo(expected.constantMin), context);
            Assert.That(actual.constantMax, Is.EqualTo(expected.constantMax), context);
            AssertAnimationCurveMatches(expected.curveMin, actual.curveMin, context);
            AssertAnimationCurveMatches(expected.curveMax, actual.curveMax, context);
        }

        private static void AssertAnimationCurveMatches(
            AnimationCurve expected,
            AnimationCurve actual,
            string context)
        {
            if (expected == null || actual == null)
            {
                Assert.That(actual == null, Is.EqualTo(expected == null), context);
                return;
            }

            Assert.That(actual.keys.Select(key => (key.time, key.value)),
                Is.EqualTo(expected.keys.Select(key => (key.time, key.value))), context);
        }
    }
}

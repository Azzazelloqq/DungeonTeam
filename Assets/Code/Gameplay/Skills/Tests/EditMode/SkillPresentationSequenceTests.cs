using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class SkillPresentationSequenceTests
    {
        [Test]
        public void CreateAnimationCue_WithNegativeDelay_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SkillActorAnimationCue(
                    SkillPresentationPhase.Start,
                    -0.01f,
                    ActorSkillAnimationCue.Cast));
        }

        [Test]
        public void CreateImpactVfx_WithNonImpactAnchor_Throws()
        {
            var prefab = new GameObject("VfxPrefab");
            try
            {
                Assert.Throws<ArgumentException>(() =>
                    new SkillVfxCue(
                        SkillPresentationPhase.Impact,
                        0f,
                        0.2f,
                        SkillVfxAnchor.TargetHit,
                        followAnchor: false,
                        prefab));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void CreateVfxCue_WithPositionOffset_ExposesOffset()
        {
            var prefab = new GameObject("VfxPrefab");
            var expectedOffset = new Vector3(0.25f, 1.5f, -0.75f);
            try
            {
                var cue = new SkillVfxCue(
                    SkillPresentationPhase.Start,
                    0f,
                    0.2f,
                    SkillVfxAnchor.SourceOrigin,
                    followAnchor: true,
                    positionOffset: expectedOffset,
                    scaleMultiplier: 1f,
                    rotationOffsetEuler: Vector3.zero,
                    prefab);

                Assert.That(cue.PositionOffset, Is.EqualTo(expectedOffset));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void CreateVfxCue_WithNonFinitePositionOffset_Throws()
        {
            var prefab = new GameObject("VfxPrefab");
            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new SkillVfxCue(
                        SkillPresentationPhase.Start,
                        0f,
                        0.2f,
                        SkillVfxAnchor.SourceOrigin,
                        followAnchor: true,
                        positionOffset: new Vector3(float.NaN, 0f, 0f),
                        scaleMultiplier: 1f,
                        rotationOffsetEuler: Vector3.zero,
                        prefab));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void CreateSequence_WithMissingCue_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new SkillPresentationSequence(
                    new SkillActorAnimationCue[] { null },
                    Array.Empty<SkillVfxCue>()));
        }
    }
}

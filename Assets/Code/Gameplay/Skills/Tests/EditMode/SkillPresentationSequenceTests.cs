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
        public void CreateSequence_WithMissingCue_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new SkillPresentationSequence(
                    new SkillActorAnimationCue[] { null },
                    Array.Empty<SkillVfxCue>()));
        }
    }
}

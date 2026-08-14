using System;
using DungeonTeam.Gameplay.Skills.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class SkillUseTimelineTests
    {
        [TestCase(-0.01f, 0f)]
        [TestCase(0f, -0.01f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void CreateTiming_WithInvalidDuration_Throws(
            float commitDelay,
            float recoveryDuration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SkillUseTiming(commitDelay, recoveryDuration));
        }

        [Test]
        public void Advance_ReachesCommit_ReportsCommitExactlyOnce()
        {
            var timeline = new SkillUseTimeline(new SkillUseTiming(0.4f, 0.2f));

            var beforeCommit = timeline.Advance(0.39f);
            var commit = timeline.Advance(0.01f);
            var recovery = timeline.Advance(0.1f);

            Assert.That(beforeCommit.Committed, Is.False);
            Assert.That(commit.Committed, Is.True);
            Assert.That(recovery.Committed, Is.False);
            Assert.That(timeline.HasCommitted, Is.True);
            Assert.That(timeline.Phase, Is.EqualTo(SkillUsePhase.Recovering));
        }

        [Test]
        public void Advance_LargeDelta_CrossesCommitAndCompletion()
        {
            var timeline = new SkillUseTimeline(new SkillUseTiming(0.4f, 0.2f));

            var result = timeline.Advance(0.6f);

            Assert.That(result.Committed, Is.True);
            Assert.That(result.Completed, Is.True);
            Assert.That(timeline.Phase, Is.EqualTo(SkillUsePhase.Completed));
            Assert.That(timeline.IsActive, Is.False);
        }

        [Test]
        public void Cancel_BeforeCommit_PreventsFutureCommit()
        {
            var timeline = new SkillUseTimeline(new SkillUseTiming(0.4f, 0.2f));
            timeline.Advance(0.2f);

            var cancelled = timeline.TryCancel();
            var afterCancel = timeline.Advance(1f);

            Assert.That(cancelled, Is.True);
            Assert.That(afterCancel.Committed, Is.False);
            Assert.That(timeline.HasCommitted, Is.False);
            Assert.That(timeline.Phase, Is.EqualTo(SkillUsePhase.Cancelled));
        }

        [Test]
        public void Cancel_AfterCommit_DoesNotRollbackCommittedUse()
        {
            var timeline = new SkillUseTimeline(new SkillUseTiming(0.2f, 0.3f));
            timeline.Advance(0.2f);

            var cancelled = timeline.TryCancel();
            var completed = timeline.Advance(0.3f);

            Assert.That(cancelled, Is.False);
            Assert.That(timeline.HasCommitted, Is.True);
            Assert.That(completed.Completed, Is.True);
            Assert.That(timeline.Phase, Is.EqualTo(SkillUsePhase.Completed));
        }

        [Test]
        public void CreateAreaDamageLevel_WithoutPreCommitWindow_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AreaDamageSkillLevelDefinition(
                    level: 1,
                    damage: 10,
                    range: 2f,
                    cooldown: 1f,
                    radius: 1f,
                    useTiming: default));
        }
    }
}

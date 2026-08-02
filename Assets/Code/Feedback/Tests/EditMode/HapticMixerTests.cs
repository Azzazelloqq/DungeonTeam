using DungeonTeam.Feedback.Runtime.Haptics;
using NUnit.Framework;
using UnityEngine;

namespace DungeonTeam.Feedback.Tests
{
    public sealed class HapticMixerTests
    {
        [Test]
        public void Advance_WithConcurrentImpulses_UsesStrongestMotorValue()
        {
            var mixer = new HapticMixer(capacity: 2);
            mixer.TryAdd(CreatePayload(low: 0.8f, high: 0.2f), contextIntensity: 1f);
            mixer.TryAdd(CreatePayload(low: 0.3f, high: 0.7f), contextIntensity: 1f);

            mixer.Advance(0.1f, out var low, out var high);

            Assert.That(low, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(high, Is.EqualTo(0.7f).Within(0.001f));
        }

        [Test]
        public void Advance_WhenImpulseExpires_RemovesItAndReturnsZero()
        {
            var mixer = new HapticMixer(capacity: 1);
            mixer.TryAdd(CreatePayload(low: 1f, high: 1f, duration: 0.1f), 1f);

            mixer.Advance(0.1f, out var low, out var high);

            Assert.That(mixer.ActiveCount, Is.Zero);
            Assert.That(low, Is.Zero);
            Assert.That(high, Is.Zero);
        }

        [Test]
        public void TryAdd_WhenCapacityFull_ReplacesOnlyLowerPriorityImpulse()
        {
            var mixer = new HapticMixer(capacity: 1);
            mixer.TryAdd(CreatePayload(1f, 0f, priority: 1), 1f);

            var accepted = mixer.TryAdd(CreatePayload(0f, 1f, priority: 2), 1f);
            mixer.Advance(0f, out var low, out var high);

            Assert.That(accepted, Is.EqualTo(HapticRejectionReason.None));
            Assert.That(low, Is.Zero.Within(0.001f));
            Assert.That(high, Is.EqualTo(1f).Within(0.001f));
        }

        private static HapticFeedbackPayload CreatePayload(
            float low,
            float high,
            float duration = 1f,
            int priority = 0)
        {
            return new HapticFeedbackPayload(
                duration,
                AnimationCurve.Constant(0f, 1f, low),
                AnimationCurve.Constant(0f, 1f, high),
                priority: priority);
        }
    }
}

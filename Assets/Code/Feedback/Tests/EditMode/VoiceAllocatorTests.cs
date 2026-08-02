using DungeonTeam.Feedback.Runtime.Audio;
using NUnit.Framework;

namespace DungeonTeam.Feedback.Tests
{
    public sealed class VoiceAllocatorTests
    {
        [Test]
        public void TryAcquire_DuringOwnerCooldown_RejectsSecondVoice()
        {
            var allocator = new VoiceAllocator(capacity: 2);
            var owner = new object();

            var first = allocator.TryAcquire(owner, 2, 10, 0.5d, now: 1d);
            var second = allocator.TryAcquire(owner, 2, 10, 0.5d, now: 1.25d);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.RejectionReason, Is.EqualTo(VoiceRejectionReason.Cooldown));
        }

        [Test]
        public void TryAcquire_WhenOwnerLimitReached_RejectsAdditionalVoice()
        {
            var allocator = new VoiceAllocator(capacity: 3);
            var owner = new object();

            allocator.TryAcquire(owner, 1, 10, 0d, now: 1d);
            var result = allocator.TryAcquire(owner, 1, 10, 0d, now: 2d);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(VoiceRejectionReason.OwnerLimit));
        }

        [Test]
        public void TryAcquire_WhenFullAndRequestHasHigherPriority_ReplacesOldestLowestPriority()
        {
            var allocator = new VoiceAllocator(capacity: 2);
            var oldestLowPriority = new object();
            var newerLowPriority = new object();
            var important = new object();
            allocator.TryAcquire(oldestLowPriority, 1, 1, 0d, now: 1d);
            allocator.TryAcquire(newerLowPriority, 1, 1, 0d, now: 2d);

            var result = allocator.TryAcquire(important, 1, 2, 0d, now: 3d);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Replaced, Is.True);
            Assert.That(allocator.IsOwnedBy(result.SlotIndex, important), Is.True);
            Assert.That(allocator.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void TryAcquire_WhenFullAndPriorityIsNotHigher_RejectsRequest()
        {
            var allocator = new VoiceAllocator(capacity: 1);
            allocator.TryAcquire(new object(), 1, 5, 0d, now: 1d);

            var result = allocator.TryAcquire(new object(), 1, 5, 0d, now: 2d);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(VoiceRejectionReason.Capacity));
        }
    }
}

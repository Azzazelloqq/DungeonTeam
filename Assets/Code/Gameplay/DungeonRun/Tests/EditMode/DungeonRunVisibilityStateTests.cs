using NUnit.Framework;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime.Tests
{
    public sealed class DungeonRunVisibilityStateTests
    {
        [Test]
        public void OpenDoor_FirstTime_RevealsOnlyItsAssociatedZone()
        {
            var state = new DungeonRunVisibilityState(zoneCount: 3, new[] { 1, 2 });

            var opened = state.TryOpenDoor(0, out var revealedZoneIndex);

            Assert.That(opened, Is.True);
            Assert.That(revealedZoneIndex, Is.EqualTo(1));
            Assert.That(state.IsZoneRevealed(0), Is.True);
            Assert.That(state.IsZoneRevealed(1), Is.True);
            Assert.That(state.IsZoneRevealed(2), Is.False);
        }

        [Test]
        public void OpenDoor_RepeatedForSameBoundary_DoesNotRevealAgain()
        {
            var state = new DungeonRunVisibilityState(zoneCount: 2, new[] { 1 });

            state.TryOpenDoor(0, out _);
            var openedAgain = state.TryOpenDoor(0, out var revealedZoneIndex);

            Assert.That(openedAgain, Is.False);
            Assert.That(revealedZoneIndex, Is.EqualTo(-1));
            Assert.That(state.IsZoneRevealed(1), Is.True);
        }
    }
}

using System;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.AmbientNpc.Tests.EditMode
{
    public sealed class AmbientNpcApplicationTests
    {
        [Test]
        public void Catalogs_RejectDuplicateProfilesAndLines()
        {
            Assert.Throws<ArgumentException>(() => new AmbientNpcProfileCatalog(new[]
            {
                Profile("ambient.same"), Profile("ambient.same")
            }));
            Assert.Throws<ArgumentException>(() => new DialoguePoolSnapshot("pool", new[]
            {
                new DialogueLineSnapshot("line.same", "Первая"),
                new DialogueLineSnapshot("line.same", "Вторая")
            }));
        }

        [Test]
        public void Selector_AlwaysReturnsConfiguredLine_AndSeedIsDeterministic()
        {
            var pool = new DialoguePoolSnapshot("pool", new[]
            {
                new DialogueLineSnapshot("line.a", "A"),
                new DialogueLineSnapshot("line.b", "B")
            });
            var first = new DialogueLineSelector(new Random(17));
            var second = new DialogueLineSelector(new Random(17));

            var selected = first.Select(pool);

            Assert.That(pool.Lines, Does.Contain(selected));
            Assert.That(second.Select(pool).LineId, Is.EqualTo(selected.LineId));
        }

        [Test]
        public void StateMachine_FollowsRouteAndStationaryTransitions()
        {
            var route = new AmbientNpcRoutineStateMachine();
            route.Advance(true);
            Assert.That(route.Current, Is.EqualTo(AmbientNpcRoutineState.MoveToAnchor));
            route.Advance(true);
            Assert.That(route.Current, Is.EqualTo(AmbientNpcRoutineState.FaceAnchor));
            route.Advance(true);
            Assert.That(route.Current, Is.EqualTo(AmbientNpcRoutineState.Activity));

            var stationary = new AmbientNpcRoutineStateMachine();
            stationary.Advance(false);
            Assert.That(stationary.Current, Is.EqualTo(AmbientNpcRoutineState.Activity));
            stationary.Advance(false);
            Assert.That(stationary.Current, Is.EqualTo(AmbientNpcRoutineState.Idle));
        }

        private static AmbientNpcProfileSnapshot Profile(string id) =>
            new(id, 1f, 90f, 0f, 1f, 0f, 1f, false);
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.DungeonRun.Tests
{
    public sealed class DungeonRunRouteProgressTests
    {
        private static readonly DungeonRunRoutePoint[] Route =
        {
            new(0f, 0f),
            new(5f, 0f),
            new(10f, 0f),
            new(15f, 0f)
        };

        [Test]
        public void TryReachCheckpoint_InOrder_StartsEncounterAfterPreviousCheckpoints()
        {
            var progress = new DungeonRunRouteProgress(
                Route,
                encounterStartIndex: 2,
                checkpointRadius: 1f);
            var phases = ObservePhases(progress);

            Assert.That(progress.TryReachCheckpoint(0, new DungeonRunRoutePoint(0.5f, 0f)),
                Is.True);
            Assert.That(progress.TryReachCheckpoint(1, new DungeonRunRoutePoint(5f, 0f)),
                Is.True);
            Assert.That(progress.TryReachCheckpoint(2, new DungeonRunRoutePoint(10f, 0f)),
                Is.True);

            Assert.That(progress.Phase, Is.EqualTo(DungeonRunRoutePhase.Encounter));
            Assert.That(progress.NextCheckpointIndex, Is.EqualTo(3));
            Assert.That(phases, Is.EqualTo(new[]
            {
                DungeonRunRoutePhase.Exploring,
                DungeonRunRoutePhase.Encounter
            }));
        }

        [Test]
        public void TryReachCheckpoint_WhenCheckpointIsSkippedOrReversed_IsRejected()
        {
            var progress = new DungeonRunRouteProgress(
                Route,
                encounterStartIndex: 2,
                checkpointRadius: 1f);

            var skipped = progress.TryReachCheckpoint(2, Route[2]);
            var first = progress.TryReachCheckpoint(0, Route[0]);
            var reversed = progress.TryReachCheckpoint(0, Route[0]);
            var skippedAfterFirst = progress.TryReachCheckpoint(2, Route[2]);

            Assert.That(skipped, Is.False);
            Assert.That(first, Is.True);
            Assert.That(reversed, Is.False);
            Assert.That(skippedAfterFirst, Is.False);
            Assert.That(progress.Phase, Is.EqualTo(DungeonRunRoutePhase.Exploring));
            Assert.That(progress.NextCheckpointIndex, Is.EqualTo(1));
        }

        [Test]
        public void TryReachCheckpoint_WhenPositionIsOutsidePlanarRadius_IsRejected()
        {
            var progress = new DungeonRunRouteProgress(
                Route,
                encounterStartIndex: 2,
                checkpointRadius: 1f);

            var reached = progress.TryReachCheckpoint(
                checkpointIndex: 0,
                position: new DungeonRunRoutePoint(0.8f, 0.8f));

            Assert.That(reached, Is.False);
            Assert.That(progress.Phase, Is.EqualTo(DungeonRunRoutePhase.Entering));
            Assert.That(progress.NextCheckpointIndex, Is.Zero);
        }

        [Test]
        public void CompleteEncounter_ThenReachExit_TransitionsExactlyOncePerPhase()
        {
            var progress = new DungeonRunRouteProgress(
                Route,
                encounterStartIndex: 2,
                checkpointRadius: 1f);
            var phases = ObservePhases(progress);
            progress.TryReachCheckpoint(0, Route[0]);
            progress.TryReachCheckpoint(1, Route[1]);
            progress.TryReachCheckpoint(2, Route[2]);

            var exitDuringEncounter = progress.TryReachCheckpoint(3, Route[3]);
            var encounterCompleted = progress.CompleteEncounter();
            var encounterCompletedAgain = progress.CompleteEncounter();
            var exitReached = progress.TryReachCheckpoint(3, Route[3]);
            var exitReachedAgain = progress.TryReachCheckpoint(3, Route[3]);

            Assert.That(exitDuringEncounter, Is.False);
            Assert.That(encounterCompleted, Is.True);
            Assert.That(encounterCompletedAgain, Is.False);
            Assert.That(exitReached, Is.True);
            Assert.That(exitReachedAgain, Is.False);
            Assert.That(progress.Phase, Is.EqualTo(DungeonRunRoutePhase.Completed));
            Assert.That(progress.NextCheckpointIndex, Is.EqualTo(Route.Length));
            Assert.That(phases, Is.EqualTo(new[]
            {
                DungeonRunRoutePhase.Exploring,
                DungeonRunRoutePhase.Encounter,
                DungeonRunRoutePhase.Continuing,
                DungeonRunRoutePhase.Completed
            }));
        }

        [Test]
        public void Constructor_WithEncounterOutsideOrderedRoute_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DungeonRunRouteProgress(Route, encounterStartIndex: 0, checkpointRadius: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DungeonRunRouteProgress(Route, encounterStartIndex: 3, checkpointRadius: 1f));
        }

        private static List<DungeonRunRoutePhase> ObservePhases(
            DungeonRunRouteProgress progress)
        {
            var phases = new List<DungeonRunRoutePhase>();
            progress.PhaseChanged += phases.Add;
            return phases;
        }
    }
}

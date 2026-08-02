using System;
using DungeonTeam.Gameplay.Team.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Team.Tests
{
    public sealed class CompanionFollowBrainTests
    {
        [Test]
        public void Evaluate_WhenCompanionIsFar_StartsFollowing()
        {
            var brain = new CompanionFollowBrain(
                startFollowingDistance: 3f,
                stopFollowingDistance: 1.5f);

            var state = brain.Evaluate(distanceToLeader: 4f);

            Assert.That(state, Is.EqualTo(CompanionFollowState.Following));
        }

        [Test]
        public void Evaluate_WhileFollowingAndOutsideStopDistance_ContinuesFollowing()
        {
            var brain = new CompanionFollowBrain(
                startFollowingDistance: 3f,
                stopFollowingDistance: 1.5f);
            brain.Evaluate(distanceToLeader: 4f);

            var state = brain.Evaluate(distanceToLeader: 2f);

            Assert.That(state, Is.EqualTo(CompanionFollowState.Following));
        }

        [Test]
        public void Evaluate_WhileFollowingAndInsideStopDistance_StopsFollowing()
        {
            var brain = new CompanionFollowBrain(
                startFollowingDistance: 3f,
                stopFollowingDistance: 1.5f);
            brain.Evaluate(distanceToLeader: 4f);

            var state = brain.Evaluate(distanceToLeader: 1.5f);

            Assert.That(state, Is.EqualTo(CompanionFollowState.Holding));
        }

        [Test]
        public void Create_WhenStartDistanceDoesNotExceedStopDistance_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new CompanionFollowBrain(
                    startFollowingDistance: 1.5f,
                    stopFollowingDistance: 1.5f));
        }
    }
}

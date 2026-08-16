using System;
using NUnit.Framework;
using DungeonTeam.Gameplay.PlayerProfile.Domain;

namespace DungeonTeam.Gameplay.PlayerProfile.Tests.EditMode
{
    public sealed class PlayerProfileStateTests
    {
        [Test]
        public void Create_ValidVariableRoster_PreservesSuppliedOrder()
        {
            var state = new PlayerProfileState(17, null,
                new[] { new HeroProfileState("leader", 2, "a"), new HeroProfileState("companion", 3, "b") },
                "leader", new[] { "companion" });
            Assert.That(state.Gold, Is.EqualTo(17));
            Assert.That(state.Heroes[1].ActorId, Is.EqualTo("companion"));
            Assert.That(state.CompanionActorIds, Is.EqualTo(new[] { "companion" }));
        }

        [Test]
        public void Create_LeaderRepeatedAsCompanion_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PlayerProfileState(0, null,
                new[] { new HeroProfileState("leader", 1, "a") }, "leader", new[] { "leader" }));
        }

        [Test]
        public void Create_NegativeGold_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProfileState(-1, null,
                new[] { new HeroProfileState("leader", 1, "a") }, "leader", Array.Empty<string>()));
        }

        [Test]
        public void Create_DuplicateHeroId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PlayerProfileState(0, null,
                new[] { new HeroProfileState("leader", 1, "a"), new HeroProfileState("leader", 2, "b") }, "leader", Array.Empty<string>()));
        }
    }
}

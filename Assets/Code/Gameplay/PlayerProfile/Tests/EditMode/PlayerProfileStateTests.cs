using System;
using NUnit.Framework;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;

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

        [Test]
        public void ChangeLeader_Companion_ReplacesItsOrderedSlotWithPreviousLeader()
        {
            var state = CreateThreeHeroState();

            var changed = state.ChangeLeader("second");

            Assert.That(changed.LeaderActorId, Is.EqualTo("second"));
            Assert.That(changed.CompanionActorIds, Is.EqualTo(new[] { "first" }));
            Assert.That(changed.CompanionActorIds.Count + 1, Is.EqualTo(state.CompanionActorIds.Count + 1));
        }

        [Test]
        public void ChangeLeader_Available_KeepsCompanionOrder()
        {
            var state = CreateThreeHeroState();

            var changed = state.ChangeLeader("third");

            Assert.That(changed.LeaderActorId, Is.EqualTo("third"));
            Assert.That(changed.CompanionActorIds, Is.EqualTo(new[] { "second" }));
        }

        [Test]
        public void AddRemoveAndLoadout_ProduceNewImmutableState()
        {
            var state = CreateThreeHeroState();
            var added = state.AddCompanion("third");
            var removed = added.RemoveCompanion("second");
            var loadoutChanged = removed.ChangeLoadout("third", "third.alt");

            Assert.That(added.CompanionActorIds, Is.EqualTo(new[] { "second", "third" }));
            Assert.That(removed.CompanionActorIds, Is.EqualTo(new[] { "third" }));
            Assert.That(loadoutChanged.Heroes[2].LoadoutId, Is.EqualTo("third.alt"));
            Assert.That(state.Heroes[2].LoadoutId, Is.EqualTo("c"));
        }

        [Test]
        public void Session_Commit_SavesBeforeReplacingCurrentState()
        {
            var repository = new RecordingRepository();
            var initial = CreateThreeHeroState();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(initial.Heroes, initial.LeaderActorId, initial.CompanionActorIds));
            var candidate = session.State.ChangeLoadout("third", "third.alt");
            repository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => session.Commit(candidate));

            Assert.That(session.State.Heroes[2].LoadoutId, Is.EqualTo("c"));
        }

        [Test]
        public void Session_Commit_ValidCandidate_SavesExactlyOnceAndRefreshesState()
        {
            var repository = new RecordingRepository();
            var initial = CreateThreeHeroState();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(initial.Heroes, initial.LeaderActorId, initial.CompanionActorIds));
            var initialSaveCount = repository.SaveCount;

            session.Commit(session.State.ChangeLoadout("third", "third.alt"));

            Assert.That(repository.SaveCount, Is.EqualTo(initialSaveCount + 1));
            Assert.That(session.State.Heroes[2].LoadoutId, Is.EqualTo("third.alt"));
        }

        private static PlayerProfileState CreateThreeHeroState() => new(
            0,
            null,
            new[]
            {
                new HeroProfileState("first", 1, "a"),
                new HeroProfileState("second", 1, "b"),
                new HeroProfileState("third", 1, "c")
            },
            "first",
            new[] { "second" });

        private sealed class RecordingRepository : IPlayerProfileRepository
        {
            public bool ThrowOnSave { get; set; }
            public int SaveCount { get; private set; }
            public bool TryLoad(out PlayerProfileState state)
            {
                state = null;
                return false;
            }

            public void Save(PlayerProfileState state)
            {
                SaveCount++;
                if (ThrowOnSave)
                {
                    throw new InvalidOperationException("Save failed.");
                }
            }
        }
    }
}

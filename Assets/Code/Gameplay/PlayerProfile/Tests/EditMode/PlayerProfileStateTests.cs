using System;
using NUnit.Framework;
using DungeonTeam.Gameplay.Inventory.Domain;
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

        [Test]
        public void Session_BankTerminalResult_AppliesGoldAndResourceOnce()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            var initialSaveCount = repository.SaveCount;

            var result = session.BankTerminalResult(new ProfileTerminalResultRequest(
                "run-one",
                7,
                new[] { new ProfileResourceGrant("resource.monster-crystal", 3) }));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.Receipt.RunId, Is.EqualTo("run-one"));
            Assert.That(session.State.Gold, Is.EqualTo(7));
            Assert.That(session.State.Inventory.Resources[0].Quantity, Is.EqualTo(3));
            Assert.That(repository.SaveCount, Is.EqualTo(initialSaveCount + 2));
        }

        [Test]
        public void Session_BankSameRunAgain_ReturnsAlreadyAppliedWithoutSaving()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            session.BankTerminalResult(new ProfileTerminalResultRequest(
                "run-one", 7, Array.Empty<ProfileResourceGrant>()));
            var saveCount = repository.SaveCount;

            var result = session.BankTerminalResult(new ProfileTerminalResultRequest(
                "run-one", 99, Array.Empty<ProfileResourceGrant>()));

            Assert.That(result.Status, Is.EqualTo(ProfileSettlementStatus.AlreadyApplied));
            Assert.That(result.Receipt, Is.Null);
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
            Assert.That(session.State.Gold, Is.EqualTo(7));
        }

        [Test]
        public void Session_Recovery_AppliesPersistedPendingBeforeConsumersUseState()
        {
            var repository = new RecordingRepository
            {
                LoadedState = CreateThreeHeroState().WithTerminalState(
                    new PendingTerminalResultState(
                        "run-recovery",
                        11,
                        new[] { new ResourceStackState("resource.monster-crystal", 2) }),
                    null)
            };

            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));

            Assert.That(session.State.PendingTerminalResult, Is.Null);
            Assert.That(session.State.LastAppliedRunId, Is.EqualTo("run-recovery"));
            Assert.That(session.State.Gold, Is.EqualTo(11));
            Assert.That(session.State.Inventory.Resources[0].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void Session_BankDifferentRunWhilePending_RejectsWithoutOverwrite()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            repository.FailOnSaveNumber = 3;
            Assert.Throws<InvalidOperationException>(() => session.BankTerminalResult(
                new ProfileTerminalResultRequest(
                    "run-pending",
                    5,
                    new[] { new ProfileResourceGrant("resource.monster-crystal", 1) })));
            var saveCount = repository.SaveCount;

            Assert.Throws<InvalidOperationException>(() => session.BankTerminalResult(
                new ProfileTerminalResultRequest("run-other", 2, Array.Empty<ProfileResourceGrant>())));
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
            Assert.That(session.State.PendingTerminalResult.RunId, Is.EqualTo("run-pending"));
        }

        [Test]
        public void Session_BankPersistenceFailure_DoesNotPublishReceiptOrAppliedState()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            repository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => session.BankTerminalResult(
                new ProfileTerminalResultRequest("run-failed", 9, Array.Empty<ProfileResourceGrant>())));
            Assert.That(session.State.Gold, Is.EqualTo(0));
            Assert.That(session.State.PendingTerminalResult, Is.Null);
            Assert.That(session.State.LastAppliedRunId, Is.Null);
        }

        [Test]
        public void State_SellUnequippedUniqueItem_RemovesItemAndAddsCatalogPriceAtomically()
        {
            var state = new PlayerProfileState(
                2,
                null,
                new[] { new HeroProfileState("leader", 1, "loadout") },
                "leader",
                Array.Empty<string>(),
                new InventoryState(
                    new[] { new ItemInstanceState("blade", "equipment.training-blade") },
                    Array.Empty<ResourceStackState>(),
                    new[] { new HeroEquipmentState("leader") }));

            var sold = state.SellUniqueItem("blade", 13);

            Assert.That(sold.Gold, Is.EqualTo(15));
            Assert.That(sold.Inventory.UniqueItems, Is.Empty);
            Assert.That(state.Inventory.UniqueItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void State_SellEquippedUniqueItem_RejectsWithoutChangingState()
        {
            var state = new PlayerProfileState(
                2,
                null,
                new[] { new HeroProfileState("leader", 1, "loadout") },
                "leader",
                Array.Empty<string>(),
                new InventoryState(
                    new[] { new ItemInstanceState("blade", "equipment.training-blade") },
                    Array.Empty<ResourceStackState>(),
                    new[] { new HeroEquipmentState("leader", "blade") }));

            Assert.Throws<InvalidOperationException>(() => state.SellUniqueItem("blade", 13));
            Assert.That(state.Gold, Is.EqualTo(2));
            Assert.That(state.Inventory.UniqueItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void State_SellResource_RemovesWholeStackAndAddsStackPrice()
        {
            var state = new PlayerProfileState(
                2,
                null,
                new[] { new HeroProfileState("leader", 1, "loadout") },
                "leader",
                Array.Empty<string>(),
                new InventoryState(
                    Array.Empty<ItemInstanceState>(),
                    new[] { new ResourceStackState("resource.monster-crystal", 4) },
                    new[] { new HeroEquipmentState("leader") }));

            var sold = state.SellResource("resource.monster-crystal", 20);

            Assert.That(sold.Gold, Is.EqualTo(22));
            Assert.That(sold.Inventory.Resources, Is.Empty);
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
            public int FailOnSaveNumber { get; set; }
            public int SaveCount { get; private set; }
            public PlayerProfileState LoadedState { get; set; }
            public bool TryLoad(out PlayerProfileState state)
            {
                state = LoadedState;
                LoadedState = null;
                return state != null;
            }

            public void Save(PlayerProfileState state)
            {
                SaveCount++;
                if (ThrowOnSave || SaveCount == FailOnSaveNumber)
                {
                    throw new InvalidOperationException("Save failed.");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Contracts.Application;
using DungeonTeam.Gameplay.Contracts.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Contracts.Tests.EditMode
{
    public sealed class ContractSessionTests
    {
        [Test]
        public void Accept_AvailableContract_SavesOnceBeforePublishingState()
        {
            var repository = new RecordingRepository();
            var session = new ContractSession(repository);
            var catalog = Catalog("contract.one", true);

            var result = session.Accept("contract.one", catalog);

            Assert.That(result.Accepted, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
            Assert.That(session.State.ActiveContractId, Is.EqualTo("contract.one"));
            Assert.That(repository.LastSavedState.ActiveContractId, Is.EqualTo("contract.one"));
        }

        [Test]
        public void Accept_UnknownDisabledOrSecondContract_DoesNotSave()
        {
            var repository = new RecordingRepository();
            var session = new ContractSession(repository);
            var catalog = new ContractCatalog(new[]
            {
                Definition("contract.one", true),
                Definition("contract.disabled", false)
            });

            Assert.That(session.Accept("contract.missing", catalog).Accepted, Is.False);
            Assert.That(session.Accept("contract.disabled", catalog).Accepted, Is.False);
            Assert.That(repository.SaveCount, Is.Zero);

            Assert.That(session.Accept("contract.one", catalog).Accepted, Is.True);
            Assert.That(session.Accept("contract.two", catalog).Accepted, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteActive_OnlyMatchingIdPersistsAndIsIdempotentForDuplicates()
        {
            var repository = new RecordingRepository();
            var session = new ContractSession(repository);
            var catalog = Catalog("contract.one", true);
            session.Accept("contract.one", catalog);
            var saveCount = repository.SaveCount;

            Assert.That(session.CompleteActive("contract.other").Completed, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
            Assert.That(session.State.ActiveContractId, Is.EqualTo("contract.one"));

            Assert.That(session.CompleteActive("contract.one").Completed, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount + 1));
            Assert.That(session.State.ActiveContractId, Is.Null);
            Assert.That(session.State.CompletedContractIds, Is.EqualTo(new[] { "contract.one" }));

            Assert.That(session.CompleteActive("contract.one").Completed, Is.False);
            Assert.That(session.Accept("contract.one", catalog).Accepted, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount + 1));
        }

        [Test]
        public void SaveFailure_DoesNotPublishAcceptedState()
        {
            var repository = new RecordingRepository { ThrowOnSave = true };
            var session = new ContractSession(repository);

            Assert.Throws<InvalidOperationException>(() => session.Accept(
                "contract.one",
                Catalog("contract.one", true)));

            Assert.That(session.State.ActiveContractId, Is.Null);
            Assert.That(repository.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void LoadedState_DefensivelyCopiesCompletedIds()
        {
            var completed = new List<string> { "contract.done" };
            var repository = new RecordingRepository
            {
                LoadedState = new ContractState(null, completed)
            };
            var session = new ContractSession(repository);
            completed.Clear();

            Assert.That(session.State.CompletedContractIds, Is.EqualTo(new[] { "contract.done" }));
        }

        [Test]
        public void RewardClaim_RequiresCompletedRewardAndMatchingPoint_ThenPersistsOnce()
        {
            var repository = new RecordingRepository();
            var definition = Definition(
                "contract.rewarded",
                true,
                reward: new ContractRewardDefinition(
                    4,
                    new[] { new ContractRewardResource("resource.crystal", 2) },
                    ContractRewardClaimPoint.Npc("npc.registrar"),
                    Text("reward.hint")));
            var catalog = new ContractCatalog(new[] { definition });
            var session = new ContractSession(repository);

            Assert.That(session.State.GetClaimableAt(ContractRewardClaimPoint.Npc("npc.registrar"), catalog), Is.Empty);
            session.Accept(definition.ContractId, catalog);
            Assert.That(session.MarkRewardClaimed(definition.ContractId, catalog), Is.False);
            session.CompleteActive(definition.ContractId);
            Assert.That(session.State.GetClaimableAt(ContractRewardClaimPoint.Reception, catalog), Is.Empty);
            Assert.That(session.State.GetClaimableAt(ContractRewardClaimPoint.Npc("npc.registrar"), catalog),
                Is.EqualTo(new[] { definition.ContractId }));
            Assert.That(session.MarkRewardClaimed(definition.ContractId, catalog), Is.True);
            Assert.That(session.MarkRewardClaimed(definition.ContractId, catalog), Is.False);
            Assert.That(session.State.ClaimedRewardContractIds, Is.EqualTo(new[] { definition.ContractId }));
        }

        [Test]
        public void State_RejectsClaimMarkerForIncompleteContract()
        {
            Assert.Throws<ArgumentException>(() => new ContractState(
                null,
                Array.Empty<string>(),
                new[] { "contract.incomplete" }));
        }

        private static ContractCatalog Catalog(string id, bool available) =>
            new(new[] { Definition(id, available) });

        private static ContractDefinition Definition(
            string id,
            bool available,
            ContractRewardDefinition reward = null) =>
            new(
                id,
                new ContractTextSnapshot(id + ".title", id + " title"),
                new ContractTextSnapshot(id + ".summary", id + " summary"),
                "location.dungeon",
                available,
                available ? null : new ContractTextSnapshot(id + ".disabled", "Disabled"),
                null,
                reward);

        private static ContractTextSnapshot Text(string id) => new(id, id);

        private sealed class RecordingRepository : IContractRepository
        {
            public int SaveCount { get; private set; }
            public bool ThrowOnSave { get; set; }
            public ContractState LoadedState { get; set; }
            public ContractState LastSavedState { get; private set; }

            public bool TryLoad(out ContractState state)
            {
                state = LoadedState;
                return state != null;
            }

            public void Save(ContractState state)
            {
                SaveCount++;
                if (ThrowOnSave)
                {
                    throw new InvalidOperationException("Save failed.");
                }

                LastSavedState = state;
            }
        }
    }
}

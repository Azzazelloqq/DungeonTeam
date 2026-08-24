using System;
using System.IO;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.Contracts.Infrastructure;
using LocalSaveSystem;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Contracts.Tests.EditMode
{
    public sealed class ContractPersistenceTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "DungeonTeam.Contracts.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, true);
            }
        }

        [Test]
        public void Repository_MissingKeyCreatesStateOnFirstAcceptedSave()
        {
            using var store = CreateStore();
            using var repository = new SaveStoreContractRepository(store);

            Assert.That(repository.TryLoad(out var missing), Is.False);
            Assert.That(missing, Is.Null);

            var state = new ContractState("contract.active", new[] { "contract.done" });
            repository.Save(state);

            using var reader = CreateStore();
            using var loadedRepository = new SaveStoreContractRepository(reader);
            Assert.That(loadedRepository.TryLoad(out var loaded), Is.True);
            Assert.That(loaded.ActiveContractId, Is.EqualTo("contract.active"));
            Assert.That(loaded.CompletedContractIds, Is.EqualTo(new[] { "contract.done" }));
        }

        [Test]
        public void Repository_LoadsLegacyNullCompletedCollectionAsEmpty()
        {
            using var store = CreateStore();
            var key = new SaveKey<ContractSaveV1>("guild.contracts");
            store.RegisterKey(key);
            store.Set(key, new ContractSaveV1 { ActiveContractId = "contract.active", CompletedContractIds = null });
            store.ForceSave();

            using var reader = CreateStore();
            using var repository = new SaveStoreContractRepository(reader);
            Assert.That(repository.TryLoad(out var loaded), Is.True);
            Assert.That(loaded.ActiveContractId, Is.EqualTo("contract.active"));
            Assert.That(loaded.CompletedContractIds, Is.Empty);
        }

        private SaveStore CreateStore() => new(new SaveStoreOptions(_path)
        {
            UseTaggedFormat = true,
            UseAtomicWrite = true,
            SaveOnQuit = false
        });
    }
}

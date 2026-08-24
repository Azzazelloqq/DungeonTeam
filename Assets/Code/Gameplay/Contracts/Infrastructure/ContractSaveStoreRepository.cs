using System;
using DungeonTeam.Gameplay.Contracts.Application;
using DungeonTeam.Gameplay.Contracts.Domain;
using LocalSaveSystem;
using Unity.Scripting.LifecycleManagement;

namespace DungeonTeam.Gameplay.Contracts.Infrastructure
{
    [SaveModel]
    [SaveVersion(1)]
    public sealed class ContractSaveV1
    {
        [SaveFieldId("active_contract_id")] public string ActiveContractId;
        [SaveFieldId("completed_contract_ids")] public string[] CompletedContractIds;
    }

    public sealed class ContractPersistenceException : InvalidOperationException
    {
        public ContractPersistenceException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    public sealed class SaveStoreContractRepository : IContractRepository, IDisposable
    {
        [NoAutoStaticsCleanup]
        private static readonly SaveKey<ContractSaveV1> ContractKey = new("guild.contracts");
        private readonly Func<ISaveStore> _freshStoreFactory;
        private ISaveStore _store;

        public SaveStoreContractRepository(ISaveStore store, Func<ISaveStore> freshStoreFactory = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _freshStoreFactory = freshStoreFactory;
            _store.RegisterKey(ContractKey);
        }

        public bool TryLoad(out ContractState state)
        {
            if (!_store.TryGet(ContractKey, out var dto) || dto == null ||
                (dto.ActiveContractId == null && dto.CompletedContractIds == null))
            {
                state = null;
                return false;
            }

            state = ToState(dto);
            return true;
        }

        public void Save(ContractState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            try
            {
                _store.Set(ContractKey, ToDto(state));
                _store.ForceSave();
                if (_freshStoreFactory != null)
                {
                    VerifyPersisted(state);
                }
            }
            catch (ContractPersistenceException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new ContractPersistenceException(
                    "Contract persistence failed; the previous session state was retained.",
                    exception);
            }
        }

        public void Dispose()
        {
            _store?.Dispose();
            _store = null;
        }

        private void VerifyPersisted(ContractState expected)
        {
            using var reader = _freshStoreFactory();
            reader.RegisterKey(ContractKey);
            if (!reader.TryGet(ContractKey, out var dto) || dto == null)
            {
                throw new ContractPersistenceException(
                    "Contract persistence verification did not observe the saved state.");
            }

            var observed = ToState(dto);
            if (!Equivalent(expected, observed))
            {
                throw new ContractPersistenceException(
                    "Contract persistence verification observed a different state.");
            }
        }

        private static ContractSaveV1 ToDto(ContractState state)
        {
            var completed = new string[state.CompletedContractIds.Count];
            for (var index = 0; index < completed.Length; index++)
            {
                completed[index] = state.CompletedContractIds[index];
            }

            return new ContractSaveV1
            {
                ActiveContractId = state.ActiveContractId,
                CompletedContractIds = completed
            };
        }

        private static ContractState ToState(ContractSaveV1 dto)
        {
            var completed = dto.CompletedContractIds ?? Array.Empty<string>();
            return new ContractState(dto.ActiveContractId, completed);
        }

        private static bool Equivalent(ContractState expected, ContractState observed)
        {
            if (!string.Equals(expected.ActiveContractId, observed.ActiveContractId, StringComparison.Ordinal) ||
                expected.CompletedContractIds.Count != observed.CompletedContractIds.Count)
            {
                return false;
            }

            for (var index = 0; index < expected.CompletedContractIds.Count; index++)
            {
                if (!string.Equals(
                        expected.CompletedContractIds[index],
                        observed.CompletedContractIds[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ContractPersistence : IDisposable
    {
        private readonly SaveStoreOptions _options;
        private readonly SaveRegistry _registry;
        private readonly SaveMigratorRegistry _migrators;
        private SaveStore _store;

        public ContractPersistence(SaveStoreOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _registry = SaveRegistry.CreateDefault(new SaveSerializationOptions
            {
                UseTaggedFormat = _options.UseTaggedFormat
            });
            _migrators = new SaveMigratorRegistry();
            _store = CreateStore();
            Repository = new SaveStoreContractRepository(_store, CreateStore);
        }

        public SaveStoreContractRepository Repository { get; }

        public void Dispose()
        {
            Repository.Dispose();
            _store = null;
        }

        private SaveStore CreateStore() => new(_options, _registry, _migrators);
    }
}

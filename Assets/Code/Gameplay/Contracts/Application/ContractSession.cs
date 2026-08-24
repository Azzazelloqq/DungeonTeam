using System;
using DungeonTeam.Gameplay.Contracts.Domain;

namespace DungeonTeam.Gameplay.Contracts.Application
{
    public interface IContractRepository
    {
        bool TryLoad(out ContractState state);
        void Save(ContractState state);
    }

    public sealed class ContractSession
    {
        private readonly IContractRepository _repository;

        public ContractSession(IContractRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (!repository.TryLoad(out var state))
            {
                state = new ContractState();
            }

            State = state ?? throw new InvalidOperationException("Loaded contract state is missing.");
        }

        public ContractState State { get; private set; }

        public ContractAcceptanceResult Accept(string contractId, ContractCatalog catalog)
        {
            var candidate = State.Clone();
            var result = candidate.Accept(contractId, catalog);
            if (!result.Accepted)
            {
                return result;
            }

            _repository.Save(candidate);
            State = candidate;
            return result;
        }

        public ContractCompletionResult CompleteActive(string contractId)
        {
            var candidate = State.Clone();
            var result = candidate.CompleteActive(contractId);
            if (!result.Completed)
            {
                return result;
            }

            _repository.Save(candidate);
            State = candidate;
            return result;
        }
    }
}

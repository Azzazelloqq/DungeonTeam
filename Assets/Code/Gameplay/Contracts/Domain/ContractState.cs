using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Contracts.Domain
{
    public enum ContractAcceptanceRejection
    {
        None = 0,
        UnknownContract = 1,
        AuthoredDisabled = 2,
        AlreadyCompleted = 3,
        ActiveContractExists = 4
    }

    public readonly struct ContractAcceptanceResult
    {
        private ContractAcceptanceResult(bool accepted, ContractAcceptanceRejection rejection)
        {
            Accepted = accepted;
            Rejection = rejection;
        }

        public bool Accepted { get; }
        public ContractAcceptanceRejection Rejection { get; }

        public static ContractAcceptanceResult Accept() =>
            new(true, ContractAcceptanceRejection.None);

        public static ContractAcceptanceResult Reject(ContractAcceptanceRejection rejection) =>
            new(false, rejection);
    }

    public enum ContractCompletionRejection
    {
        None = 0,
        NoActiveContract = 1,
        MismatchedContract = 2,
        AlreadyCompleted = 3
    }

    public readonly struct ContractCompletionResult
    {
        private ContractCompletionResult(bool completed, ContractCompletionRejection rejection)
        {
            Completed = completed;
            Rejection = rejection;
        }

        public bool Completed { get; }
        public ContractCompletionRejection Rejection { get; }

        public static ContractCompletionResult Complete() =>
            new(true, ContractCompletionRejection.None);

        public static ContractCompletionResult Reject(ContractCompletionRejection rejection) =>
            new(false, rejection);
    }

    public sealed class ContractState
    {
        private ReadOnlyCollection<string> _completedContractIds;

        public ContractState()
            : this(null, Array.Empty<string>())
        {
        }

        public ContractState(string activeContractId, IReadOnlyList<string> completedContractIds)
        {
            ActiveContractId = string.IsNullOrWhiteSpace(activeContractId)
                ? null
                : RequireId(activeContractId, nameof(activeContractId));
            if (completedContractIds == null)
            {
                throw new ArgumentNullException(nameof(completedContractIds));
            }

            var copy = new string[completedContractIds.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                var id = RequireId(completedContractIds[index], nameof(completedContractIds));
                if (!ids.Add(id))
                {
                    throw new ArgumentException(
                        $"Completed contract ID '{id}' is duplicated.",
                        nameof(completedContractIds));
                }

                if (id == ActiveContractId)
                {
                    throw new ArgumentException(
                        "A contract cannot be active and completed at the same time.",
                        nameof(completedContractIds));
                }

                copy[index] = id;
            }

            _completedContractIds = new ReadOnlyCollection<string>(copy);
        }

        public string ActiveContractId { get; private set; }
        public IReadOnlyList<string> CompletedContractIds => _completedContractIds;

        public ContractState Clone() => new(ActiveContractId, CompletedContractIds);

        public ContractAcceptanceResult Accept(string contractId, ContractCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (ActiveContractId != null)
            {
                return ContractAcceptanceResult.Reject(ContractAcceptanceRejection.ActiveContractExists);
            }

            ContractDefinition definition;
            try
            {
                definition = catalog.Require(contractId);
            }
            catch (ArgumentException)
            {
                return ContractAcceptanceResult.Reject(ContractAcceptanceRejection.UnknownContract);
            }
            catch (KeyNotFoundException)
            {
                return ContractAcceptanceResult.Reject(ContractAcceptanceRejection.UnknownContract);
            }

            if (ContainsCompleted(definition.ContractId))
            {
                return ContractAcceptanceResult.Reject(ContractAcceptanceRejection.AlreadyCompleted);
            }

            if (!definition.IsAuthoredAvailable)
            {
                return ContractAcceptanceResult.Reject(ContractAcceptanceRejection.AuthoredDisabled);
            }

            ActiveContractId = definition.ContractId;
            return ContractAcceptanceResult.Accept();
        }

        public bool TryAccept(string contractId, ContractCatalog catalog) =>
            Accept(contractId, catalog).Accepted;

        public ContractCompletionResult CompleteActive(string contractId)
        {
            if (ActiveContractId == null)
            {
                return ContractCompletionResult.Reject(ContractCompletionRejection.NoActiveContract);
            }

            if (string.IsNullOrWhiteSpace(contractId) ||
                !string.Equals(ActiveContractId, contractId, StringComparison.Ordinal))
            {
                return ContractCompletionResult.Reject(ContractCompletionRejection.MismatchedContract);
            }

            var completed = new string[_completedContractIds.Count + 1];
            for (var index = 0; index < _completedContractIds.Count; index++)
            {
                completed[index] = _completedContractIds[index];
            }

            completed[^1] = ActiveContractId;
            ActiveContractId = null;
            _completedContractIds = new ReadOnlyCollection<string>(completed);
            return ContractCompletionResult.Complete();
        }

        public bool IsCompleted(string contractId)
        {
            return !string.IsNullOrWhiteSpace(contractId) && ContainsCompleted(contractId);
        }

        private bool ContainsCompleted(string contractId)
        {
            for (var index = 0; index < _completedContractIds.Count; index++)
            {
                if (string.Equals(_completedContractIds[index], contractId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RequireId(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Stable ID cannot be empty.", parameterName);
    }
}

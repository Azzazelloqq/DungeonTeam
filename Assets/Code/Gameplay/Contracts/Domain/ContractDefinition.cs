using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Contracts.Domain
{
    public sealed class ContractTextSnapshot
    {
        public ContractTextSnapshot(string textId, string displayText)
        {
            TextId = RequireId(textId, nameof(textId));
            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Contract text cannot be empty.", nameof(displayText));
            }

            DisplayText = displayText;
        }

        public string TextId { get; }
        public string DisplayText { get; }

        private static string RequireId(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Stable ID cannot be empty.", parameterName);
    }

    public sealed class ContractDefinition
    {
        public ContractDefinition(
            string contractId,
            ContractTextSnapshot title,
            ContractTextSnapshot summary,
            string locationId,
            bool isAuthoredAvailable,
            ContractTextSnapshot authoredDisabledReason,
            string minimumRankId = null)
        {
            ContractId = RequireId(contractId, nameof(contractId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            LocationId = RequireId(locationId, nameof(locationId));
            MinimumRankId = string.IsNullOrWhiteSpace(minimumRankId)
                ? null
                : RequireId(minimumRankId, nameof(minimumRankId));

            if (isAuthoredAvailable && authoredDisabledReason != null)
            {
                throw new ArgumentException(
                    "An authored-available contract cannot have a disabled reason.",
                    nameof(authoredDisabledReason));
            }

            if (!isAuthoredAvailable && authoredDisabledReason == null)
            {
                throw new ArgumentException(
                    "An authored-disabled contract requires a disabled reason.",
                    nameof(authoredDisabledReason));
            }

            IsAuthoredAvailable = isAuthoredAvailable;
            AuthoredDisabledReason = authoredDisabledReason;
        }

        public string ContractId { get; }
        public ContractTextSnapshot Title { get; }
        public ContractTextSnapshot Summary { get; }
        public string LocationId { get; }
        public bool IsAuthoredAvailable { get; }
        public ContractTextSnapshot AuthoredDisabledReason { get; }
        public string MinimumRankId { get; }

        private static string RequireId(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Stable ID cannot be empty.", parameterName);
    }

    public sealed class ContractCatalog
    {
        private readonly IReadOnlyDictionary<string, ContractDefinition> _definitionsById;

        public ContractCatalog(IReadOnlyList<ContractDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copy = new ContractDefinition[definitions.Count];
            var byId = new Dictionary<string, ContractDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Contract definition at index {index} is missing.",
                    nameof(definitions));
                if (!byId.TryAdd(definition.ContractId, definition))
                {
                    throw new ArgumentException(
                        $"Contract ID '{definition.ContractId}' is duplicated.",
                        nameof(definitions));
                }

                copy[index] = definition;
            }

            Definitions = new ReadOnlyCollection<ContractDefinition>(copy);
            _definitionsById = new ReadOnlyDictionary<string, ContractDefinition>(byId);
        }

        public IReadOnlyList<ContractDefinition> Definitions { get; }

        public ContractDefinition Require(string contractId)
        {
            var id = !string.IsNullOrWhiteSpace(contractId)
                ? contractId
                : throw new ArgumentException("Contract ID cannot be empty.", nameof(contractId));
            if (!_definitionsById.TryGetValue(id, out var definition))
            {
                throw new KeyNotFoundException($"Unknown contract ID '{id}'.");
            }

            return definition;
        }

        public void ValidateSupportedLocations(IReadOnlyCollection<string> supportedLocationIds)
        {
            if (supportedLocationIds == null)
            {
                throw new ArgumentNullException(nameof(supportedLocationIds));
            }

            var locations = new HashSet<string>(supportedLocationIds, StringComparer.Ordinal);
            for (var index = 0; index < Definitions.Count; index++)
            {
                var definition = Definitions[index];
                if (!locations.Contains(definition.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Contract '{definition.ContractId}' references unsupported location '{definition.LocationId}'.");
                }
            }
        }
    }
}

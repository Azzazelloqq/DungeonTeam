using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Contracts.Domain
{
    public enum ContractRewardClaimPointKind
    {
        Reception = 0,
        Npc = 1
    }

    public sealed class ContractRewardClaimPoint
    {
        private ContractRewardClaimPoint(ContractRewardClaimPointKind kind, string npcId)
        {
            if (!Enum.IsDefined(typeof(ContractRewardClaimPointKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (kind == ContractRewardClaimPointKind.Npc && string.IsNullOrWhiteSpace(npcId))
            {
                throw new ArgumentException("NPC reward claim point requires an NPC ID.", nameof(npcId));
            }

            if (kind == ContractRewardClaimPointKind.Reception && npcId != null)
            {
                throw new ArgumentException("Reception reward claim point cannot target an NPC.", nameof(npcId));
            }

            Kind = kind;
            NpcId = npcId;
        }

        public ContractRewardClaimPointKind Kind { get; }
        public string NpcId { get; }

        public static ContractRewardClaimPoint Reception =>
            new(ContractRewardClaimPointKind.Reception, null);

        public static ContractRewardClaimPoint Npc(string npcId) =>
            new(ContractRewardClaimPointKind.Npc, RequireId(npcId, nameof(npcId)));

        public bool Matches(ContractRewardClaimPoint other) =>
            other != null && Kind == other.Kind &&
            string.Equals(NpcId, other.NpcId, StringComparison.Ordinal);

        private static string RequireId(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Stable ID cannot be empty.", parameterName);
    }

    public sealed class ContractRewardResource
    {
        public ContractRewardResource(string definitionId, int amount)
        {
            DefinitionId = RequireId(definitionId, nameof(definitionId));
            Amount = amount > 0 ? amount : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public string DefinitionId { get; }
        public int Amount { get; }

        private static string RequireId(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Stable ID cannot be empty.", parameterName);
    }

    public sealed class ContractRewardDefinition
    {
        private readonly ReadOnlyCollection<ContractRewardResource> _resources;

        public ContractRewardDefinition(
            long goldAmount,
            IReadOnlyList<ContractRewardResource> resources,
            ContractRewardClaimPoint claimPoint,
            ContractTextSnapshot claimHint)
        {
            if (goldAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goldAmount));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            ClaimPoint = claimPoint ?? throw new ArgumentNullException(nameof(claimPoint));
            ClaimHint = claimHint ?? throw new ArgumentNullException(nameof(claimHint));

            var copy = new ContractRewardResource[resources.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = resources[index] ?? throw new ArgumentException(
                    "Reward resource is missing.", nameof(resources));
                if (!ids.Add(copy[index].DefinitionId))
                {
                    throw new ArgumentException(
                        $"Reward resource '{copy[index].DefinitionId}' is duplicated.",
                        nameof(resources));
                }
            }

            if (goldAmount == 0 && copy.Length == 0)
            {
                throw new ArgumentException(
                    "Reward must contain Gold or at least one resource.",
                    nameof(resources));
            }

            GoldAmount = goldAmount;
            _resources = Array.AsReadOnly(copy);
        }

        public long GoldAmount { get; }
        public IReadOnlyList<ContractRewardResource> Resources => _resources;
        public ContractRewardClaimPoint ClaimPoint { get; }
        public ContractTextSnapshot ClaimHint { get; }
    }

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
            string minimumRankId = null,
            ContractRewardDefinition reward = null)
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
            Reward = reward;
        }

        public string ContractId { get; }
        public ContractTextSnapshot Title { get; }
        public ContractTextSnapshot Summary { get; }
        public string LocationId { get; }
        public bool IsAuthoredAvailable { get; }
        public ContractTextSnapshot AuthoredDisabledReason { get; }
        public string MinimumRankId { get; }
        public ContractRewardDefinition Reward { get; }

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

        public bool Contains(string contractId) =>
            !string.IsNullOrWhiteSpace(contractId) && _definitionsById.ContainsKey(contractId);

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
